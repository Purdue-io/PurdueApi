using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PurdueIo.Database;
using PurdueIo.Database.Models;
using PurdueIo.Scraper;
using System;
using System.Collections.Generic;
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
using ScrapedMeeting = PurdueIo.Scraper.Models.Meeting;
using ScrapedSection = PurdueIo.Scraper.Models.Section;

namespace PurdueIo.CatalogSync
{
    public class FastSync
    {
        public record SyncProgress (double Progress, string Description);

        public enum TermSyncBehavior
        {
            SyncAllTerms,
            SyncNewAndCurrentTerms,
        }

        public static async Task SynchronizeAsync(IScraper scraper, ApplicationDbContext dbContext,
            ILogger<FastSync> logger,
            TermSyncBehavior termSyncBehavior = TermSyncBehavior.SyncAllTerms,
            Action<SyncProgress> progress = null)
        {
            await (new FastSync(scraper, dbContext, logger)).InternalSynchronizeAsync(
                termSyncBehavior, progress: progress);
        }

        public static async Task SynchronizeAsync(IScraper scraper, ApplicationDbContext dbContext,
            ILogger<FastSync> logger, IEnumerable<string> termCodes,
            TermSyncBehavior termSyncBehavior = TermSyncBehavior.SyncAllTerms,
            Action<SyncProgress> progress = null)
        {
            await (new FastSync(scraper, dbContext, logger)).InternalSynchronizeAsync(
                TermSyncBehavior.SyncAllTerms, termCodes, progress: progress);
        }

        public static async Task SynchronizeAsync(IScraper scraper, ApplicationDbContext dbContext,
            ILogger<FastSync> logger, IEnumerable<string> termCodes,
            IEnumerable<string> subjectCodes,
            TermSyncBehavior termSyncBehavior = TermSyncBehavior.SyncAllTerms,
            Action<SyncProgress> progress = null)
        {
            await (new FastSync(scraper, dbContext, logger)).InternalSynchronizeAsync(
                termSyncBehavior, termCodes, subjectCodes, progress);
        }

        private readonly IScraper scraper;

        private readonly ApplicationDbContext dbContext;

        private readonly ILogger<FastSync> logger;

        private readonly TimeSpan MEETING_TIME_EQUALITY_TOLERANCE = TimeSpan.FromMinutes(1);

        private const double PROGRESS_UPDATING_TERM_LIST = 0.05;

        private const double PROGRESS_UPDATING_TERM_SECTIONS = 0.95;

        private const double PROGRESS_TERM_UPDATING_SUBJECT_LIST = 0.05;

        private const double PROGRESS_TERM_UPDATING_SUBJECTS = 0.95;
        
        private const double PROGRESS_SUBJECT_CACHING_ENTITIES = 0.05;

        private const double PROGRESS_SUBJECT_SCRAPING_SECTIONS = 0.05;

        private const double PROGRESS_SUBJECT_UPDATING_CLASSES = 0.90;

        private Dictionary<string, DatabaseCampus> dbCachedCampuses =
            new Dictionary<string, DatabaseCampus>();

        private Dictionary<(string number, string title), DatabaseCourse> dbCachedCourses = 
            new Dictionary<(string number, string title), DatabaseCourse>();

        private Dictionary<(Guid campusId, string buildingCode), DatabaseBuilding> dbCachedBuildings
            = new Dictionary<(Guid campusId, string buildingCode), DatabaseBuilding>();

        private Dictionary<string, DatabaseInstructor> dbCachedInstructors =
            new Dictionary<string, DatabaseInstructor>();

        private FastSync(IScraper scraper, ApplicationDbContext dbContext,
            ILogger<FastSync> logger)
        {
            this.scraper = scraper;
            this.dbContext = dbContext;
            this.logger = logger;

            // Disable change tracking
            this.dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
            this.dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        private async Task InternalSynchronizeAsync(TermSyncBehavior termSyncBehavior,
            IEnumerable<string> termCodes = null, IEnumerable<string> subjectCodes = null,
            Action<SyncProgress> progress = null)
        {
            // Fetch existing terms from DB
            var terms = dbContext.Terms.ToDictionary(t => t.Code);

            // Scrape new terms from service
            progress?.Invoke(new (0, "Updating term list..."));
            var scrapedTerms = await scraper.GetTermsAsync();
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
                await InternalSynchronizeTermAsync(term, subjectCodes, termProgress);
                ++numTermsSynced;
            }
            progress?.Invoke(new (1.0, $"Synchronized {termsToSync.Count} terms!"));
        }

        private async Task InternalSynchronizeTermAsync(DatabaseTerm term,
            IEnumerable<string> subjectCodes = null, Action<SyncProgress> progress = null)
        {
            // Fetch existing subjects from DB
            var subjects = dbContext.Subjects.ToDictionary(s => s.Abbreviation);

            // Scrape new subjects from service
            var scrapedSubjects = await scraper.GetSubjectsAsync(term.Code);
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
                await InternalSynchronizeTermSubjectAsync(term, subject, subjectProgress);
                ++numSubjectsSynced;
            }

            // Update term's start and end date to match the earliest and latest meeting
            if (dbContext.Sections.Where(s => s.Class.TermId == term.Id).Count() > 0)
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

            // Scrape new sections, group into classes
            progress?.Invoke(new (PROGRESS_SUBJECT_CACHING_ENTITIES, "Scraping sections..."));
            var scrapedSections = await scraper.GetSectionsAsync(term.Code, subject.Abbreviation);
            var groupedSections = SectionLinker.GroupLinkedSections(scrapedSections);

            // Sync each section group as a "Class"
            var syncedSectionGroups = 0;
            foreach (var sectionGroup in groupedSections)
            {
                var sectionCourseNumber = sectionGroup
                    .FirstOrDefault(s => (s.CourseNumber.Length > 0))
                    .CourseNumber;
                var sectionCourseTitle = sectionGroup
                    .FirstOrDefault(s => (s.CourseTitle.Length > 0))
                    .CourseTitle;
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
            var scrapedSectionsByCrn = scrapedSections.ToDictionary(s => s.Crn);
            var crnsToRemove = existingCrns
                .Where(s => !scrapedSectionsByCrn.ContainsKey(s));
            foreach (var crnToRemove in crnsToRemove)
            {
                var sectionToRemove = dbContext.Sections.SingleOrDefault(s =>
                    (s.Class.TermId == term.Id) &&
                    (s.Crn == crnToRemove));
                if (sectionToRemove != null)
                {
                    dbContext.Entry(sectionToRemove).State = EntityState.Deleted;
                }
            }

            dbContext.SaveChanges();

            // Remove classes that no longer have any sections
            var removedClasses = dbContext.Classes.Where(c =>
                (c.TermId == term.Id) &&
                (c.Course.SubjectId == subject.Id) &&
                (c.Sections.Count == 0));
            foreach (var removedClass in removedClasses)
            {
                dbContext.Entry(removedClass).State = EntityState.Deleted;
            }
            dbContext.SaveChanges();
        }

        private void InternalSynchronizeClass(DatabaseTerm term, DatabaseSubject subject,
            ICollection<ScrapedSection> sectionGroup)
        {
            // Hydrate campus
            var campusCode = "";
            var campusName = "";
            var sectionWithCampus = sectionGroup.FirstOrDefault(s => s.CampusCode.Length > 0);
            if (sectionWithCampus != null)
            {
                campusCode = sectionWithCampus.CampusCode;
                campusName = sectionWithCampus.CampusName;
            }
            DatabaseCampus campus = FetchOrAddCampus(campusCode, campusName);

            // Hydrate course
            var sectionWithCourse = sectionGroup
                .FirstOrDefault(s => 
                    (s.CourseNumber.Length > 0) && 
                    (s.CourseTitle.Length > 0));
            if (sectionWithCourse == null)
            {
                logger.LogError("WARNING: No course information found for CRNs " + 
                    $"{string.Join(", ", sectionGroup.Select(s => s.Crn))}");
                return;
            }
            var creditHours = sectionGroup
                .OrderByDescending(c => c.CreditHours)
                .FirstOrDefault()
                .CreditHours;
            DatabaseCourse course = FetchOrAddCourse(subject, sectionWithCourse.CourseNumber,
                sectionWithCourse.CourseTitle, creditHours, sectionWithCourse.Description);

            // Hydrate class
            Guid classId = Guid.Empty;
            var crns = sectionGroup.Select(s => s.Crn);
            var dbSections = dbContext.Sections
                .Where(s => (s.Class.TermId == term.Id) && (crns.Contains(s.Crn)))
                .ToList();
            if (dbSections.Count == 0)
            {
                var newClass = new DatabaseClass()
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
                classId = dbSections.First().ClassId;
            }

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
                campus = dbContext.Campuses.SingleOrDefault(c => (c.Code == campusCode));
                if (campus != null)
                {
                    // Sometimes the campus name will change despite having the same code
                    if (campus.Name != campusName)
                    {
                        campus.Name = campusName;
                        var campusEntry = dbContext.Entry(campus);
                        campusEntry.Property(s => s.Name).CurrentValue = campusName;
                        campusEntry.State = EntityState.Modified;
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
            string courseTitle, double creditHours, string courseDescription)
        {
            DatabaseCourse course;
            var courseKey = (number: courseNumber, title: courseTitle);
            if (dbCachedCourses.ContainsKey(courseKey))
            {
                course = dbCachedCourses[courseKey];
            }
            else
            {
                course = dbContext.Courses.SingleOrDefault(c => 
                    (c.SubjectId == subject.Id) &&
                    (c.Number == courseNumber) && 
                    (c.Title == courseTitle));
                if (course == null)
                {
                    course = new DatabaseCourse()
                    {
                        Id = Guid.NewGuid(),
                        Number = courseNumber,
                        SubjectId = subject.Id,
                        Title = courseTitle,
                        CreditHours = creditHours,
                        Description = courseDescription,
                    };
                    dbContext.Add(course);
                }
                dbCachedCourses[courseKey] = course;
            }
            return course;
        }

        private DatabaseSection AddOrUpdateSection(Guid classId,
            ICollection<DatabaseSection> dbSections, ScrapedSection section)
        {
            var startDate = section.Meetings
                .Select(m => m.StartDate)
                .Where(d => d != null)
                .OrderBy(d => d)
                .FirstOrDefault();

            var endDate = section.Meetings
                .Select(m => m.EndDate)
                .Where(d => d != null)
                .OrderByDescending(d => d)
                .FirstOrDefault();

            var existingSection = dbSections.SingleOrDefault(s => (s.Crn == section.Crn));
            if (existingSection != null)
            {
                var modified = false;
                var dbEntry = dbContext.Entry(existingSection);
                if (existingSection.ClassId != classId)
                {
                    existingSection.ClassId = classId;
                    dbEntry.Property(s => s.ClassId).CurrentValue = classId;
                    modified = true;
                }
                if (existingSection.Type != section.Type)
                {
                    existingSection.Type = section.Type;
                    dbEntry.Property(s => s.Type).CurrentValue = section.Type;
                    modified = true;
                }
                // TODO: Registration status..?
                if (existingSection.StartDate != startDate)
                {
                    existingSection.StartDate = startDate;
                    dbEntry.Property(s => s.StartDate).CurrentValue = startDate;
                    modified = true;
                }
                if (existingSection.EndDate != endDate)
                {
                    existingSection.EndDate = endDate;
                    dbEntry.Property(s => s.EndDate).CurrentValue = endDate;
                    modified = true;
                }

                if (modified)
                {
                    dbEntry.State = EntityState.Modified;
                }
                return existingSection;
            }
            else
            {
                var newSection = new DatabaseSection()
                {
                    Id = Guid.NewGuid(),
                    Crn = section.Crn,
                    ClassId = classId,
                    Type = section.Type,
                    StartDate = startDate,
                    EndDate = endDate,
                };
                dbContext.Add(newSection);
                return newSection;
            }
        }

        private void AddOrUpdateSectionMeetings(DatabaseCampus campus, DatabaseSection dbSection,
            ScrapedSection section)
        {
            var dbMeetings = dbContext.Meetings
                .Where(m => (m.SectionId == dbSection.Id))
                .ToList();
            var existingMeetingsToKeep = new List<DatabaseMeeting>();
            foreach (var meeting in section.Meetings)
            {
                var meetingDuration = TimeSpan.Zero;
                if ((meeting.EndTime != null) && (meeting.StartTime != null))
                {
                    meetingDuration = ((TimeOnly)meeting.EndTime - (TimeOnly)meeting.StartTime);
                }
                var dbRoom = FetchOrAddMeetingRoom(campus, meeting);
                var dbInstructors = FetchOrAddMeetingInstructors(meeting);

                // MyPurdue doesn't expose any sort of unique ID for each meeting,
                // so we need to determine equality via a heuristic:
                // If it happens at the same time at the same location, it is the same
                // meeting.
                var dbMeeting = dbMeetings.FirstOrDefault(m => 
                    (m.RoomId == dbRoom.Id) &&
                    (m.DaysOfWeek == (DatabaseDaysOfWeek) meeting.DaysOfWeek) &&
                    (m.StartDate == meeting.StartDate) &&
                    (m.EndDate == meeting.EndDate) &&
                    (m.StartTime == meeting.StartTime) &&
                    (m.Duration == meetingDuration));

                if (dbMeeting == null)
                {
                    dbMeeting = new DatabaseMeeting()
                    {
                        Id = Guid.NewGuid(),
                        SectionId = dbSection.Id,
                        Type = meeting.Type,
                        StartDate = meeting.StartDate,
                        EndDate = meeting.EndDate,
                        DaysOfWeek = (DatabaseDaysOfWeek) meeting.DaysOfWeek,
                        StartTime = meeting.StartTime,
                        Duration = meetingDuration,
                        RoomId = dbRoom.Id,
                        Instructors = new List<DatabaseInstructor>(),
                    };
                    dbContext.Add(dbMeeting);
                }
                else
                {
                    existingMeetingsToKeep.Add(dbMeeting);
                }

                var dbMeetingInstructors = dbContext.Instructors
                    .Where(i => i.Meetings.Any(m => (m.Id == dbMeeting.Id))).ToList();

                var instructorsToRemove = dbMeetingInstructors.Where(i => 
                    dbInstructors.SingleOrDefault(di => di.Id == i.Id) == null)
                    .ToList();
                foreach (var instructorToRemove in instructorsToRemove)
                {
                    dbContext.Remove(new MeetingInstructor() 
                    {
                        MeetingId = dbMeeting.Id,
                        InstructorId = instructorToRemove.Id 
                    });
                }
                foreach (var dbInstructor in dbInstructors)
                {
                    if (dbMeetingInstructors.SingleOrDefault(i => 
                        (i.Id == dbInstructor.Id)) == null)
                    {
                        dbContext.Add(new MeetingInstructor()
                        {
                            MeetingId = dbMeeting.Id,
                            InstructorId = dbInstructor.Id,
                        });
                    }
                }
            }

            // Remove any "orphaned" meetings
            var dbMeetingsToRemove = dbMeetings.Where(m => !existingMeetingsToKeep.Contains(m));
            foreach (var dbMeetingToRemove in dbMeetingsToRemove)
            {
                dbContext.Entry(dbMeetingToRemove).State = EntityState.Deleted;
            }
        }

        private DatabaseRoom FetchOrAddMeetingRoom(DatabaseCampus campus, ScrapedMeeting meeting)
        {
            var dbBuilding = FetchOrAddMeetingBuilding(campus, meeting);
            var dbRoom = dbBuilding.Rooms.FirstOrDefault(r => (r.Number == meeting.RoomNumber));
            if (dbRoom == null)
            {
                dbRoom = new DatabaseRoom()
                {
                    Id = Guid.NewGuid(),
                    Number = meeting.RoomNumber,
                    BuildingId = dbBuilding.Id,
                };
                dbContext.Add(dbRoom);
                dbBuilding.Rooms.Add(dbRoom);
            }
            return dbRoom;
        }

        private DatabaseBuilding FetchOrAddMeetingBuilding(DatabaseCampus campus,
            ScrapedMeeting meeting)
        {
            var buildingKey = (campusId: campus.Id, code: meeting.BuildingCode);
            if (dbCachedBuildings.ContainsKey(buildingKey))
            {
                return dbCachedBuildings[buildingKey];
            }
            else
            {
                var dbBuilding = dbContext.Buildings
                    .Include(b => b.Rooms)
                    .SingleOrDefault(b =>
                        (b.CampusId == campus.Id) &&
                        (b.ShortCode == meeting.BuildingCode));
                if (dbBuilding == null)
                {
                    dbBuilding = new DatabaseBuilding()
                    {
                        Id = Guid.NewGuid(),
                        CampusId = campus.Id,
                        Name = meeting.BuildingName,
                        ShortCode = meeting.BuildingCode,
                        Rooms = new List<DatabaseRoom>(),
                    };
                    dbContext.Add(dbBuilding);
                }
                dbCachedBuildings[buildingKey] = dbBuilding;
                return dbBuilding;
            }
        }

        private ICollection<DatabaseInstructor> FetchOrAddMeetingInstructors(ScrapedMeeting meeting)
        {
            var returnVal = new List<DatabaseInstructor>();
            foreach (var scrapedInstructor in meeting.Instructors)
            {
                if (dbCachedInstructors.ContainsKey(scrapedInstructor.email))
                {
                    returnVal.Add(dbCachedInstructors[scrapedInstructor.email]);
                }
                else
                {
                    var dbInstructor = dbContext.Instructors.SingleOrDefault(i =>
                        (i.Email == scrapedInstructor.email));
                    if (dbInstructor == null)
                    {
                        dbInstructor = new DatabaseInstructor()
                        {
                            Id = Guid.NewGuid(),
                            Name = scrapedInstructor.name,
                            Email = scrapedInstructor.email,
                        };
                        dbContext.Add(dbInstructor);
                    }
                    dbCachedInstructors[scrapedInstructor.email] = dbInstructor;
                    returnVal.Add(dbInstructor);
                }
            }
            return returnVal;
        }
    }
}