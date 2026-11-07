using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PurdueIo.Database;
using PurdueIo.Scraper.Connections;
using PurdueIo.Scraper.Connections.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using DatabaseBuilding = PurdueIo.Database.Models.Building;
using DatabaseCampus = PurdueIo.Database.Models.Campus;
using DatabaseClass = PurdueIo.Database.Models.Class;
using DatabaseCourse = PurdueIo.Database.Models.Course;
using DatabaseDaysOfWeek = PurdueIo.Database.Models.DaysOfWeek;
using DatabaseInstructor = PurdueIo.Database.Models.Instructor;
using DatabaseMeeting = PurdueIo.Database.Models.Meeting;
using DatabaseRoom = PurdueIo.Database.Models.Room;
using DatabaseSection = PurdueIo.Database.Models.Section;
using DatabaseSubject = PurdueIo.Database.Models.Subject;
using DatabaseTerm = PurdueIo.Database.Models.Term;

namespace PurdueIo.CatalogSync
{
    public class FastSync
    {
        public record SyncProgress (double Progress, string Description);

        // A term or subject that failed to sync. SubjectCode is null when the whole term
        // failed (e.g. its subject list couldn't be fetched).
        public record SyncFailure (string TermCode, string SubjectCode, Exception Exception);

        public record SyncResult (IReadOnlyList<SyncFailure> Failures)
        {
            public bool Succeeded => Failures.Count == 0;
        }

        // Thrown when too many consecutive terms/subjects fail, which usually means MyPurdue
        // or the database is down rather than one subject being bad.
        public class SyncAbortedException : Exception
        {
            public SyncAbortedException(string message, Exception innerException)
                : base(message, innerException) { }
        }

        public enum TermSyncBehavior
        {
            SyncAllTerms,
            SyncNewAndCurrentTerms,
        }

        public static async Task<SyncResult> SynchronizeAsync(MyPurdueConnection connection,
            ApplicationDbContext dbContext, ILogger<FastSync> logger,
            TermSyncBehavior termSyncBehavior = TermSyncBehavior.SyncAllTerms,
            Action<SyncProgress> progress = null)
        {
            return await (new FastSync(connection, dbContext, logger)).InternalSynchronizeAsync(
                termSyncBehavior, progress: progress);
        }

        public static async Task<SyncResult> SynchronizeAsync(MyPurdueConnection connection,
            ApplicationDbContext dbContext, ILogger<FastSync> logger, IEnumerable<string> termCodes,
            TermSyncBehavior termSyncBehavior = TermSyncBehavior.SyncAllTerms,
            Action<SyncProgress> progress = null)
        {
            return await (new FastSync(connection, dbContext, logger)).InternalSynchronizeAsync(
                termSyncBehavior, termCodes, progress: progress);
        }

        public static async Task<SyncResult> SynchronizeAsync(MyPurdueConnection connection,
            ApplicationDbContext dbContext, ILogger<FastSync> logger, IEnumerable<string> termCodes,
            IEnumerable<string> subjectCodes,
            TermSyncBehavior termSyncBehavior = TermSyncBehavior.SyncAllTerms,
            Action<SyncProgress> progress = null)
        {
            return await (new FastSync(connection, dbContext, logger)).InternalSynchronizeAsync(
                termSyncBehavior, termCodes, subjectCodes, progress);
        }

        private readonly MyPurdueConnection connection;

        private readonly ApplicationDbContext dbContext;

        private readonly ILogger<FastSync> logger;

        private const double PROGRESS_UPDATING_TERM_LIST = 0.05;

        private const double PROGRESS_UPDATING_TERM_SECTIONS = 0.95;

        private const double PROGRESS_TERM_UPDATING_SUBJECT_LIST = 0.05;

        private const double PROGRESS_TERM_UPDATING_SUBJECTS = 0.95;
        
        private const double PROGRESS_SUBJECT_CACHING_ENTITIES = 0.05;

        private const double PROGRESS_SUBJECT_SCRAPING_SECTIONS = 0.05;

        private const double PROGRESS_SUBJECT_UPDATING_CLASSES = 0.90;

        private const string Tba = "TBA";

        // Stop the run once this many terms/subjects in a row have failed.
        public const int MAX_CONSECUTIVE_FAILURES = 5;

        private readonly List<SyncFailure> failures = new();

        private int consecutiveFailures = 0;

        private readonly Dictionary<string, string> campusCodesByName =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<Guid> claimedClassIds = new();

        private Dictionary<string, DatabaseCampus> dbCachedCampuses =
            new Dictionary<string, DatabaseCampus>();

        private Dictionary<(string subjectCode, string number, string title), DatabaseCourse>
            dbCachedCourses = 
                new Dictionary<(string subjectCode, string number, string title), DatabaseCourse>();

        private Dictionary<(Guid campusId, string buildingCode), DatabaseBuilding> dbCachedBuildings
            = new Dictionary<(Guid campusId, string buildingCode), DatabaseBuilding>();

        private Dictionary<string, DatabaseInstructor> dbCachedInstructors =
            new Dictionary<string, DatabaseInstructor>();

        private FastSync(MyPurdueConnection connection, ApplicationDbContext dbContext,
            ILogger<FastSync> logger)
        {
            this.connection = connection;
            this.dbContext = dbContext;
            this.logger = logger;
        }

        private async Task<SyncResult> InternalSynchronizeAsync(TermSyncBehavior termSyncBehavior,
            IEnumerable<string> termCodes = null, IEnumerable<string> subjectCodes = null,
            Action<SyncProgress> progress = null)
        {
            // Fetch existing terms from DB
            var terms = dbContext.Terms.ToDictionary(t => t.Code);

            // Scrape new terms from service
            progress?.Invoke(new (0, "Updating term list..."));
            var scrapedTerms = await connection.GetTermListAsync();
            var termsToSync = new List<DatabaseTerm>();

            // Add any new terms to the database
            foreach (var scrapedTerm in scrapedTerms)
            {
                // Apply filter to terms if specified
                if ((termCodes?.Count() > 0) && termCodes.All(t => (t != scrapedTerm.Id)))
                {
                    continue;
                }
                if (!terms.ContainsKey(scrapedTerm.Id))
                {
                    var dbTerm = new DatabaseTerm()
                    {
                        Id = Guid.NewGuid(),
                        Code = scrapedTerm.Id,
                        Name = scrapedTerm.Name,
                        StartDate = null,
                        EndDate = null,
                    };
                    dbContext.Add(dbTerm);
                    terms[scrapedTerm.Id] = dbTerm;
                }

                if (termSyncBehavior == TermSyncBehavior.SyncAllTerms)
                {
                    termsToSync.Add(terms[scrapedTerm.Id]);
                }
                else if (termSyncBehavior == TermSyncBehavior.SyncNewAndCurrentTerms)
                {
                    // We only care about terms that haven't ended yet - 
                    // we still want to sync "future" terms.
                    if (terms[scrapedTerm.Id].EndDate == null ||
                        (terms[scrapedTerm.Id].EndDate >
                            DateOnly.FromDateTime(DateTime.Now.AddDays(-1))))
                    {
                        termsToSync.Add(terms[scrapedTerm.Id]);
                    }
                }
                else
                {
                    throw new ArgumentException("Invalid term sync behavior specified");
                }
            }
            dbContext.SaveChanges();
            progress?.Invoke(new (PROGRESS_UPDATING_TERM_LIST, $"Updated {terms.Count} terms"));

            var termListStr = string.Join('\n', termsToSync.Select(t => $"{t.Code} / {t.Name}"));
            progress?.Invoke(new (PROGRESS_UPDATING_TERM_LIST, $"Syncing terms:\n{termListStr}"));

            // Sync each term
            int numTermsSynced = 0;
            foreach (var term in termsToSync)
            {
                // Translate each term's sync progress to overall progress before reporting it out
                Action<SyncProgress> termProgress = (s) => progress?.Invoke(
                    new (PROGRESS_UPDATING_TERM_LIST + 
                        (PROGRESS_UPDATING_TERM_SECTIONS / termsToSync.Count) * 
                        ((double)numTermsSynced + (s.Progress)),
                    $"{term.Code}: {s.Description}"));

                progress?.Invoke(new (PROGRESS_UPDATING_TERM_LIST + 
                    ((PROGRESS_UPDATING_TERM_SECTIONS / termsToSync.Count) * 
                        (double)numTermsSynced),
                    $"Synchronizing {term.Code} / {term.Name}"));
                try
                {
                    // Not resetting consecutiveFailures here: only a subject that actually
                    // synced proves MyPurdue and the DB are healthy.
                    await InternalSynchronizeTermAsync(term, subjectCodes, termProgress);
                }
                catch (Exception e) when (e is not SyncAbortedException)
                {
                    RecordFailure(term, null, e);
                }
                ++numTermsSynced;
            }

            if (failures.Count == 0)
            {
                progress?.Invoke(new (1.0, $"Synchronized {termsToSync.Count} terms!"));
            }
            else
            {
                progress?.Invoke(new (1.0, $"Synchronized {termsToSync.Count} terms with " +
                    $"{failures.Count} failure(s): " + string.Join(", ",
                        failures.Select(f => $"{f.TermCode} {f.SubjectCode ?? "(term)"}"))));
            }
            return new SyncResult(failures.ToList());
        }

        // Logs a failed term/subject, discards its unsaved changes so they can't leak into
        // the next SaveChanges, and starts a fresh MyPurdue session (stale search state is a
        // common cause of failures). Aborts the run after too many failures in a row.
        private void RecordFailure(DatabaseTerm term, DatabaseSubject subject, Exception e)
        {
            logger.LogError(e, "Failed to synchronize {Term} {Subject}; continuing with the rest",
                term.Code, subject?.Abbreviation ?? "(term)");
            failures.Add(new SyncFailure(term.Code, subject?.Abbreviation, e));
            ResetSyncState();
            connection.ResetSession();

            if (++consecutiveFailures >= MAX_CONSECUTIVE_FAILURES)
            {
                throw new SyncAbortedException(
                    $"Aborting sync after {consecutiveFailures} consecutive failures " +
                    $"({failures.Count} total)", e);
            }
        }

        // Forgets every tracked entity and per-subject cache. Cached entities are tracked
        // instances, so the caches must be cleared together with the change tracker.
        private void ResetSyncState()
        {
            dbContext.ChangeTracker.Clear();
            dbCachedCampuses.Clear();
            dbCachedCourses.Clear();
            dbCachedBuildings.Clear();
            dbCachedInstructors.Clear();
            claimedClassIds.Clear();
        }

        private async Task InternalSynchronizeTermAsync(DatabaseTerm term,
            IEnumerable<string> subjectCodes = null, Action<SyncProgress> progress = null)
        {
            // Fetch existing subjects from DB
            var subjects = dbContext.Subjects.ToDictionary(s => s.Abbreviation);

            // Scrape new subjects from service
            var scrapedSubjects = await connection.GetSubjectListAsync(term.Code);
            var subjectsToSync = new List<DatabaseSubject>();

            // Add any new subjects to the database
            progress?.Invoke(new (0.0, "Updating subjects..."));
            foreach (var scrapedSubject in scrapedSubjects)
            {
                // If a subject was specified, filter to that subject only
                if ((subjectCodes?.Count() > 0) && subjectCodes.All(s => (s != scrapedSubject.Code)))
                {
                    continue;
                }
                if (!subjects.ContainsKey(scrapedSubject.Code))
                {
                    var dbSubject = new DatabaseSubject()
                    {
                        Id = Guid.NewGuid(),
                        Name = scrapedSubject.Name,
                        Abbreviation = scrapedSubject.Code,
                    };
                    dbContext.Add(dbSubject);
                    subjects[scrapedSubject.Code] = dbSubject;
                }
                subjectsToSync.Add(subjects[scrapedSubject.Code]);
            }
            dbContext.SaveChanges();
            progress?.Invoke(new (PROGRESS_TERM_UPDATING_SUBJECT_LIST,
                $"Updated {subjectsToSync.Count} subjects"));

            // Sync each subject
            var numSubjectsSynced = 0;
            foreach (var subject in subjectsToSync)
            {
                // Translate each subjects's sync progress to term progress before reporting it out
                Action<SyncProgress> subjectProgress = (s) => progress?.Invoke(
                    new (PROGRESS_TERM_UPDATING_SUBJECT_LIST + 
                        (PROGRESS_TERM_UPDATING_SUBJECTS / subjectsToSync.Count) * 
                        ((double)numSubjectsSynced + (s.Progress)),
                    $"{subject.Abbreviation}: {s.Description}"));

                progress?.Invoke(new (PROGRESS_TERM_UPDATING_SUBJECT_LIST + 
                    ((PROGRESS_TERM_UPDATING_SUBJECTS / subjectsToSync.Count) *
                        (double)numSubjectsSynced),
                    $"Synchronizing {subject.Abbreviation} / {subject.Name}"));
                try
                {
                    await InternalSynchronizeTermSubjectAsync(term, subject, subjectProgress);
                    consecutiveFailures = 0;
                }
                catch (Exception e) when (e is not SyncAbortedException)
                {
                    RecordFailure(term, subject, e);
                }
                ++numSubjectsSynced;
            }

            // Update term's start and end date to match the earliest and latest meeting
            if (dbContext.Sections.Where(s => s.Class.TermId == term.Id).Any())
            {
                dbContext.Entry(term).Property(t => t.StartDate).CurrentValue = 
                    dbContext.Sections
                        .Where(s => (s.Class.TermId == term.Id) && (s.StartDate != null))
                        .Select(s => s.StartDate)
                        .OrderBy(d => d)
                        .FirstOrDefault();
                dbContext.Entry(term).Property(t => t.EndDate).CurrentValue =
                    dbContext.Sections
                        .Where(s => (s.Class.TermId == term.Id) && (s.EndDate != null))
                        .Select(s => s.EndDate)
                        .OrderByDescending(d => d)
                        .FirstOrDefault();
                dbContext.Entry(term).State = EntityState.Modified;
                dbContext.SaveChanges();
            }
        }

        private async Task InternalSynchronizeTermSubjectAsync(DatabaseTerm term,
            DatabaseSubject subject, Action<SyncProgress> progress = null)
        {
            // Fetch existing campuses, buildings, instructors
            progress?.Invoke(new (0.0, "Retrieving existing CRNs..."));
            // Fetch existing courses, sections for this term + subject
            var existingCrns = dbContext.Sections
                .Where(s => 
                    (s.Class.TermId == term.Id) && 
                    (s.Class.Course.SubjectId == subject.Id))
                .Select(s => s.Crn)
                .ToList();

            // Scrape all sections for the given term/subject
            progress?.Invoke(new (PROGRESS_SUBJECT_CACHING_ENTITIES, "Scraping sections..."));
            var scrapedSections = await connection.GetSectionsAsync(term.Code,
                subject.Abbreviation);

            // Populate campus code <-> name map
            foreach (var t in scrapedSections.SelectMany(s => s.Meetings ?? [])
                .Select(m => m.MeetingTime))
            {
                if (!string.IsNullOrEmpty(t?.CampusCode) && !string.IsNullOrEmpty(t.CampusName))
                    campusCodesByName[t.CampusName] = t.CampusCode;
            }

            // Group linked sections into classes
            var groupedSections = await connection.GroupLinkedSectionsAsync(term.Code,
                scrapedSections);

            // Sync each section group as a "Class"
            var syncedSectionGroups = 0;
            foreach (var sectionGroup in groupedSections)
            {
                var sectionCourseNumber = sectionGroup
                    .FirstOrDefault(s => !string.IsNullOrEmpty(s.CourseNumber))
                    ?.CourseNumber;
                var sectionCourseTitle = sectionGroup
                    .FirstOrDefault(s => !string.IsNullOrEmpty(s.CourseTitle))
                    ?.CourseTitle;
                progress?.Invoke(new (PROGRESS_SUBJECT_CACHING_ENTITIES + 
                    PROGRESS_SUBJECT_SCRAPING_SECTIONS +
                    ((PROGRESS_SUBJECT_UPDATING_CLASSES / groupedSections.Count)
                        * (double)syncedSectionGroups),
                    $"Synchronizing {sectionCourseNumber} {sectionCourseTitle}"));
                InternalSynchronizeClass(term, subject, sectionGroup);
                syncedSectionGroups++;
            }

            // Delete any existing CRNs that are no longer present in the latest scraped sections
            progress?.Invoke(new ((PROGRESS_SUBJECT_CACHING_ENTITIES + 
                PROGRESS_SUBJECT_SCRAPING_SECTIONS +
                PROGRESS_SUBJECT_UPDATING_CLASSES),
                $"Cleaning up outdated sections..."));
            var scrapedCrns = scrapedSections.Select(s => s.Crn).ToHashSet();
            var crnsToRemove = existingCrns
                .Where(s => !scrapedCrns.Contains(s))
                .ToList();
            if (crnsToRemove.Count > 0)
            {
                // One query for all of them; meetings and instructor links cascade in the DB.
                dbContext.Sections.RemoveRange(dbContext.Sections.Where(s =>
                    (s.Class.TermId == term.Id) &&
                    crnsToRemove.Contains(s.Crn)));
            }

            dbContext.SaveChanges();

            // Remove classes that no longer have any sections
            var removedClasses = dbContext.Classes.Where(c =>
                (c.TermId == term.Id) &&
                (c.Course.SubjectId == subject.Id) &&
                (c.Sections.Count == 0));
            dbContext.Classes.RemoveRange(removedClasses.ToList());

            dbContext.SaveChanges();
            ResetSyncState();
        }

        private static (string Code, string Name)? GetCampus(ICollection<MyPurdueSection> sectionGroup) =>
            sectionGroup
                .SelectMany(s => s.Meetings ?? [])
                .Select(m => m.MeetingTime)
                .Where(t => !string.IsNullOrEmpty(t?.CampusCode) &&
                    !string.IsNullOrEmpty(t.CampusName))
                .GroupBy(t => (Code: t.CampusCode, Name: t.CampusName))
                .OrderByDescending(g => g.Count())
                .Select(g => ((string Code, string Name)?)g.Key)
                .FirstOrDefault();

        private static double? EffectiveCreditHours(MyPurdueSection s) =>
            s.CreditHours ?? s.CreditHourHigh ?? s.CreditHourLow;

        private void InternalSynchronizeClass(DatabaseTerm term, DatabaseSubject subject,
            ICollection<MyPurdueSection> sectionGroup)
        {
            var campusSearch = GetCampus(sectionGroup);
            if (campusSearch is null)
            {
                var name = sectionGroup.Select(s => s.CampusName)
                    .FirstOrDefault(n => !string.IsNullOrEmpty(n));
                if (name != null && campusCodesByName.TryGetValue(name, out var code))
                    campusSearch = (code, name);
            }
            if (campusSearch is null)
            {
                logger.LogWarning("No campus found for CRNs {Crns}",
                    string.Join(", ", sectionGroup.Select(s => s.Crn)));
            }
            var (campusCode, campusName) = campusSearch ?? ("", "");
            DatabaseCampus campus = FetchOrAddCampus(campusCode, campusName);

            // Hydrate course
            var sectionWithCourse = sectionGroup
                .FirstOrDefault(s => 
                    !string.IsNullOrEmpty(s.CourseNumber) && 
                    !string.IsNullOrEmpty(s.CourseTitle));
            if (sectionWithCourse == null)
            {
                logger.LogError("WARNING: No course information found for CRNs " + 
                    $"{string.Join(", ", sectionGroup.Select(s => s.Crn))}");
                return;
            }
            var creditHours = sectionGroup.Max(EffectiveCreditHours);
            DatabaseCourse course = FetchOrAddCourse(subject, sectionWithCourse.CourseNumber,
                sectionWithCourse.CourseTitle, creditHours, "");
            // TODO: Fetch an actual course description separately

            // Hydrate class
            var crns = sectionGroup.Select(s => s.Crn);
            var dbSections = dbContext.Sections
                .Where(s => (s.Class.TermId == term.Id) && (crns.Contains(s.Crn)))
                .ToList();
            // Re-use the existing class that holds most of this group's sections,
            // unless another group in this subject already claimed it.
            var reusableClassId = dbSections
                .GroupBy(s => s.ClassId)
                .OrderByDescending(g => g.Count())
                .Select(g => (Guid?)g.Key)
                .FirstOrDefault(id => !claimedClassIds.Contains(id.Value));

            Guid classId;
            if (reusableClassId is null)
            {
                var newClass = new DatabaseClass
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    TermId = term.Id,
                    CampusId = campus.Id,
                };
                dbContext.Classes.Add(newClass);
                classId = newClass.Id;
            }
            else
            {
                classId = reusableClassId.Value;
                var dbSectionGroupClass = dbContext.Classes.Single(c => c.Id == classId);
                dbSectionGroupClass.CourseId = course.Id;
                dbSectionGroupClass.CampusId = campus.Id;
            }
            claimedClassIds.Add(classId);

            // Hydrate each section
            foreach (var section in sectionGroup)
            {
                var dbSection = AddOrUpdateSection(classId, dbSections, section);
                AddOrUpdateSectionMeetings(campus, dbSection, section);
            }
        }

        private DatabaseCampus FetchOrAddCampus(string campusCode, string campusName)
        {
            DatabaseCampus campus;
            if (dbCachedCampuses.ContainsKey(campusCode))
            {
                campus = dbCachedCampuses[campusCode];
            }
            else
            {
                campus = dbContext.Campuses.FirstOrDefault(c => (c.Code == campusCode));
                if (campus != null)
                {
                    // Sometimes the campus name will change despite having the same code
                    if (campus.Name != campusName)
                    {
                        campus.Name = campusName;
                    }
                }
                else
                {
                    campus = new DatabaseCampus()
                    {
                        Id = Guid.NewGuid(),
                        Code = campusCode,
                        Name = campusName,
                        ZipCode = "",
                    };
                    dbContext.Add(campus);
                }
                dbCachedCampuses[campusCode] = campus;
            }
            return campus;
        }

        private DatabaseCourse FetchOrAddCourse(DatabaseSubject subject, string courseNumber,
            string courseTitle, double? creditHours, string courseDescription)
        {
            DatabaseCourse course;
            var courseKey = (subjectCode: subject.Abbreviation,
                number: courseNumber, title: courseTitle);
            if (!dbCachedCourses.TryGetValue(courseKey, out course))
            {
                course = dbContext.Courses.FirstOrDefault(c =>
                    (c.SubjectId == subject.Id) &&
                    (c.Number == courseNumber) &&
                    (c.Title == courseTitle));
                if (course == null)
                {
                    course = new DatabaseCourse
                    {
                        Id = Guid.NewGuid(),
                        Number = courseNumber,
                        SubjectId = subject.Id,
                        Title = courseTitle,
                        Description = courseDescription,
                        CreditHours = creditHours ?? 0,
                    };
                    dbContext.Add(course);
                }
                dbCachedCourses[courseKey] = course;
            }
            if (creditHours.HasValue)
            {
                course.CreditHours = creditHours.Value;
            }
            return course;
        }

        private static DateOnly? ParseMyPurdueDate(string? value) =>
            DateOnly.TryParseExact(value, "MM/dd/yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date)
                ? date
                : null;

        private static TimeOnly? ParseMyPurdueTime(string? value) =>
            TimeOnly.TryParseExact(value, "HHmm", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var time)
                ? time
                : null;

        private static DatabaseDaysOfWeek GetDaysOfWeek(MyPurdueMeetingTime t)
        {
            var days = DatabaseDaysOfWeek.None;
            if (t.IsSunday)    days |= DatabaseDaysOfWeek.Sunday;
            if (t.IsMonday)    days |= DatabaseDaysOfWeek.Monday;
            if (t.IsTuesday)   days |= DatabaseDaysOfWeek.Tuesday;
            if (t.IsWednesday) days |= DatabaseDaysOfWeek.Wednesday;
            if (t.IsThursday)  days |= DatabaseDaysOfWeek.Thursday;
            if (t.IsFriday)    days |= DatabaseDaysOfWeek.Friday;
            if (t.IsSaturday)  days |= DatabaseDaysOfWeek.Saturday;
            return days;
        }

        private DatabaseSection AddOrUpdateSection(Guid classId,
            ICollection<DatabaseSection> dbSections, MyPurdueSection section)
        {
            var meetingTimes = (section.Meetings ?? [])
                .Select(m => m.MeetingTime)
                .Where(t => t != null)
                .ToList();

            var startDate = meetingTimes.Select(t => ParseMyPurdueDate(t.StartDate)).Min();
            var endDate = meetingTimes.Select(t => ParseMyPurdueDate(t.EndDate)).Max();

            var existingSection = dbSections.FirstOrDefault(s => s.Crn == section.Crn);
            if (existingSection != null)
            {
                existingSection.ClassId = classId;
                existingSection.Type = section.SectionType;
                existingSection.StartDate = startDate;
                existingSection.EndDate = endDate;
                // TODO: Registration status..?
                return existingSection;
            }

            var newSection = new DatabaseSection
            {
                Id = Guid.NewGuid(),
                Crn = section.Crn,
                ClassId = classId,
                Type = section.SectionType,
                StartDate = startDate,
                EndDate = endDate,
            };
            dbContext.Add(newSection);
            return newSection;
        }

        private void AddOrUpdateSectionMeetings(DatabaseCampus campus, DatabaseSection dbSection,
            MyPurdueSection section)
        {
            var dbMeetings = dbContext.Meetings
                .Include(m => m.Instructors)
                .Where(m => m.SectionId == dbSection.Id)
                .ToList();

            // Existing meetings not yet matched to a scraped one; whatever is left gets deleted.
            var unmatched = new List<DatabaseMeeting>(dbMeetings);

            // Banner 9 search results only list faculty per section, not per meeting.
            var dbInstructors = FetchOrAddSectionInstructors(section);

            var meetingTimes = (section.Meetings ?? [])
                .Select(m => m.MeetingTime)
                .Where(t => t != null);

            foreach (var meetingTime in meetingTimes)
            {
                var startDate = ParseMyPurdueDate(meetingTime.StartDate);
                var endDate = ParseMyPurdueDate(meetingTime.EndDate);
                var startTime = ParseMyPurdueTime(meetingTime.BeginTime);
                var endTime = ParseMyPurdueTime(meetingTime.EndTime);
                var duration = (startTime is { } start && endTime is { } end) ?
                    end - start : TimeSpan.Zero;
                var daysOfWeek = GetDaysOfWeek(meetingTime);
                var roomId = FetchOrAddMeetingRoom(campus, meetingTime).Id;

                // MyPurdue has no meeting ID, so match on time + place.
                var dbMeeting = unmatched.FirstOrDefault(m =>
                    m.RoomId == roomId &&
                    m.DaysOfWeek == daysOfWeek &&
                    m.StartDate == startDate &&
                    m.EndDate == endDate &&
                    m.StartTime == startTime &&
                    m.Duration == duration);

                if (dbMeeting == null)
                {
                    dbMeeting = new DatabaseMeeting
                    {
                        Id = Guid.NewGuid(),
                        SectionId = dbSection.Id,
                        Type = section.SectionType,
                        StartDate = startDate,
                        EndDate = endDate,
                        DaysOfWeek = daysOfWeek,
                        StartTime = startTime,
                        Duration = duration,
                        RoomId = roomId,
                        Instructors = new List<DatabaseInstructor>(),
                    };
                    dbContext.Add(dbMeeting);
                }
                else
                {
                    unmatched.Remove(dbMeeting); // so two identical scraped meetings can't claim the same row
                    dbMeeting.Type = section.SectionType;
                }

                // Sync instructors via the navigation; EF adds/deletes MeetingInstructor rows.
                foreach (var stale in dbMeeting.Instructors.Where(i => 
                    dbInstructors.All(d => d.Id != i.Id)).ToList())
                {
                    dbMeeting.Instructors.Remove(stale);
                }

                foreach (var dbInstructor in dbInstructors)
                {
                    if (dbMeeting.Instructors.All(i => i.Id != dbInstructor.Id))
                    {
                        dbMeeting.Instructors.Add(dbInstructor);
                    }
                }
            }

            dbContext.Meetings.RemoveRange(unmatched);
        }

        private DatabaseRoom FetchOrAddMeetingRoom(DatabaseCampus campus,
            MyPurdueMeetingTime meetingTime)
        {
            var hasLocation = !string.IsNullOrWhiteSpace(meetingTime.BuildingCode)
                && !string.IsNullOrWhiteSpace(meetingTime.Room);

            var buildingCode = hasLocation ? meetingTime.BuildingCode.Trim() : Tba;
            var buildingName = hasLocation ? (meetingTime.BuildingName ?? buildingCode) : Tba;
            var roomNumber = hasLocation ? meetingTime.Room.Trim() : Tba;

            var dbBuilding = FetchOrAddMeetingBuilding(campus, buildingCode, buildingName);
            var dbRoom = dbBuilding.Rooms.FirstOrDefault(r => r.Number == roomNumber);
            if (dbRoom == null)
            {
                dbRoom = new DatabaseRoom
                {
                    Id = Guid.NewGuid(),
                    Number = roomNumber,
                    BuildingId = dbBuilding.Id,
                };
                dbContext.Add(dbRoom);
                dbBuilding.Rooms.Add(dbRoom);
            }
            return dbRoom;
        }

        private DatabaseBuilding FetchOrAddMeetingBuilding(DatabaseCampus campus,
            string buildingCode, string buildingName)
        {
            var buildingKey = (campusId: campus.Id, buildingCode: buildingCode);
            if (dbCachedBuildings.TryGetValue(buildingKey, out var cached))
            {
                return cached;
            }

            var dbBuilding = dbContext.Buildings
                .Include(b => b.Rooms)
                .FirstOrDefault(b => b.CampusId == campus.Id && b.ShortCode == buildingCode);

            if (dbBuilding == null)
            {
                dbBuilding = new DatabaseBuilding
                {
                    Id = Guid.NewGuid(),
                    CampusId = campus.Id,
                    Name = buildingName,
                    ShortCode = buildingCode,
                    Rooms = new List<DatabaseRoom>(),
                };
                dbContext.Add(dbBuilding);
            }

            dbCachedBuildings[buildingKey] = dbBuilding;
            return dbBuilding;
        }

        // Banner 9 search results only list faculty per section, so every meeting
        // in a section gets the section's instructors.
        private ICollection<DatabaseInstructor> FetchOrAddSectionInstructors(
            MyPurdueSection section)
        {
            var returnVal = new List<DatabaseInstructor>();
            foreach (var faculty in section.Faculty ?? [])
            {
                if (string.IsNullOrWhiteSpace(faculty.Email))
                {
                    continue;
                }
                var email = faculty.Email.Trim();

                if (!dbCachedInstructors.TryGetValue(email, out var dbInstructor))
                {
                    dbInstructor = dbContext.Instructors.FirstOrDefault(i => i.Email == email);
                    if (dbInstructor == null)
                    {
                        dbInstructor = new DatabaseInstructor
                        {
                            Id = Guid.NewGuid(),
                            Name = faculty.Name,
                            Email = email,
                        };
                        dbContext.Add(dbInstructor);
                    }
                    dbCachedInstructors[email] = dbInstructor;
                }

                if (!returnVal.Contains(dbInstructor))
                {
                    returnVal.Add(dbInstructor);
                }
            }
            return returnVal;
        }
    }
}