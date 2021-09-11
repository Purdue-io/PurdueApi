using Microsoft.EntityFrameworkCore;
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
        public record SyncProgress (double? Progress, string Description);

        public enum TermSyncBehavior
        {
            SyncAllTerms,
            // TODO: SyncCurrentAndNewTerms
        }

        public static async Task SynchronizeAsync(IScraper scraper, ApplicationDbContext dbContext,
            TermSyncBehavior termSyncBehavior = TermSyncBehavior.SyncAllTerms,
            IProgress<SyncProgress> progress = null)
        {
            await (new FastSync(scraper, dbContext)).InternalSynchronizeAsync(termSyncBehavior,
                progress: progress);
        }

        public static async Task SynchronizeAsync(IScraper scraper, ApplicationDbContext dbContext,
            string termCode, IProgress<SyncProgress> progress = null)
        {
            await (new FastSync(scraper, dbContext)).InternalSynchronizeAsync(
                TermSyncBehavior.SyncAllTerms, termCode, progress: progress);
        }

        public static async Task SynchronizeAsync(IScraper scraper, ApplicationDbContext dbContext,
            string termCode, string subjectCode,
            IProgress<SyncProgress> progress = null)
        {
            await (new FastSync(scraper, dbContext)).InternalSynchronizeAsync(
                TermSyncBehavior.SyncAllTerms, termCode, subjectCode, progress);
        }

        private IScraper scraper;

        private ApplicationDbContext dbContext;

        private readonly TimeSpan MEETING_TIME_EQUALITY_TOLERANCE = TimeSpan.FromMinutes(1);

        private const double PROGRESS_UPDATING_TERM_LIST = 0.05;

        private const double PROGRESS_UPDATING_TERM_SECTIONS = 0.95;
        
        private FastSync(IScraper scraper, ApplicationDbContext dbContext)
        {
            this.scraper = scraper;
            this.dbContext = dbContext;

            // Disable change tracking
            this.dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
            this.dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        private async Task InternalSynchronizeAsync(TermSyncBehavior termSyncBehavior,
            string termCode = "", string subjectCode = "",
            IProgress<SyncProgress> progress = null)
        {
            // Fetch existing terms from DB
            var terms = dbContext.Terms.ToDictionary(t => t.Code);

            // Scrape new terms from service
            progress?.Report(new (0, "Updating term list..."));
            var scrapedTerms = await scraper.GetTermsAsync();

            // Add any new terms to the database
            foreach (var scrapedTerm in scrapedTerms)
            {
                // If a term code was specified, filter to that term only
                if ((termCode.Length > 0) && (scrapedTerm.Id != termCode))
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
                        StartDate = DateTimeOffset.MinValue,
                        EndDate = DateTimeOffset.MaxValue,
                    };
                    dbContext.Add(dbTerm);
                    terms[scrapedTerm.Id] = dbTerm;
                }
            }
            dbContext.SaveChanges();
            progress?.Report(new (PROGRESS_UPDATING_TERM_LIST, $"Updated {terms.Count} terms"));

            // Sync each term
            int numTermsSynced = 0;
            foreach (var termPair in terms)
            {
                // If a term code was specified, filter to that term only
                if ((termCode.Length > 0) && (termPair.Key != termCode))
                {
                    continue;
                }
                progress?.Report(new (null, $"Synchronizing {termPair.Key}"));
                await InternalSynchronizeTermAsync(termPair.Value, subjectCode);
                ++numTermsSynced;
                progress?.Report(new (
                    PROGRESS_UPDATING_TERM_LIST + 
                        (PROGRESS_UPDATING_TERM_SECTIONS / terms.Count) * (double)numTermsSynced,
                    $"Synchronized {termPair.Key}"));
            }
        }

        private async Task InternalSynchronizeTermAsync(DatabaseTerm term, string subjectCode)
        {
            // Fetch existing subjects from DB
            var subjects = dbContext.Subjects.ToDictionary(s => s.Abbreviation);

            // Scrape new subjects from service
            var scrapedSubjects = await scraper.GetSubjectsAsync(term.Code);

            // Add any new subjects to the database
            foreach (var scrapedSubject in scrapedSubjects)
            {
                // If a subject was specified, filter to that subject only
                if ((subjectCode.Length > 0) && (scrapedSubject.Code != subjectCode))
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
            }
            dbContext.SaveChanges();

            // Sync each subject
            foreach (var subjectPair in subjects)
            {
                // If a subject code was specified, filter to that subject only
                if ((subjectCode.Length > 0) && (subjectPair.Key != subjectCode))
                {
                    continue;
                }
                await InternalSynchronizeTermSubjectAsync(term, subjectPair.Value);
            }

            // Update term's start and end date to match the earliest and latest meeting
            if (dbContext.Sections.Where(s => s.Class.TermId == term.Id).Count() > 0)
            {
                dbContext.Entry(term).Property(t => t.StartDate).CurrentValue = 
                    dbContext.Sections
                        .Where(s => s.Class.TermId == term.Id)
                        .Select(s => s.StartDate)
                        .OrderBy(d => d)
                        .First();
                dbContext.Entry(term).Property(t => t.EndDate).CurrentValue =
                    dbContext.Sections
                        .Where(s => s.Class.TermId == term.Id)
                        .Select(s => s.EndDate)
                        .OrderByDescending(d => d)
                        .First();
                dbContext.SaveChanges();
            }
        }

        private async Task InternalSynchronizeTermSubjectAsync(DatabaseTerm term,
            DatabaseSubject subject)
        {
            // Fetch existing campuses, buildings, instructors
            var campuses = dbContext.Campuses.ToDictionary(c => c.Code);
            var buildings = dbContext.Buildings
                .Include(b => b.Rooms)
                .ToDictionary(b => (campusId: b.CampusId, code: b.ShortCode));
            var instructors = dbContext.Instructors.ToDictionary(i => i.Email);
            // Fetch existing courses, sections for this term + subject
            var courses = dbContext.Courses.Where(c => c.SubjectId == subject.Id)
                .ToDictionary(c => (number: c.Number, name: c.Title));
            var sections = dbContext.Sections
                .Include(s => s.Meetings)
                .ThenInclude(m => m.Room)
                .ThenInclude(r => r.Building)
                .Include(s => s.Meetings)
                .ThenInclude(m => m.Instructors)
                .Where(s => 
                    (s.Class.TermId == term.Id) && 
                    (s.Class.Course.SubjectId == subject.Id))
                .ToDictionary(s => s.Crn);

            // Scrape new sections, group into classes
            var scrapedSections = await scraper.GetSectionsAsync(term.Code, subject.Abbreviation);
            var groupedSections = SectionLinker.GroupLinkedSections(scrapedSections);

            // Sync each section group as a "Class"
            foreach (var sectionGroup in groupedSections)
            {
                InternalSynchronizeClass(term, subject, campuses, buildings, instructors, courses,
                    sections, sectionGroup);
            }

            // Delete any existing CRNs that are no longer present in the latest scraped sections
            var scrapedSectionsByCrn = scrapedSections.ToDictionary(s => s.Crn);
            var removedSections = sections
                .Where(s => !scrapedSectionsByCrn.ContainsKey(s.Key)).Select(s => s.Value);
            foreach (var removedSection in removedSections)
            {
                dbContext.Entry(removedSection).State = EntityState.Deleted;
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
            Dictionary<string, DatabaseCampus> campuses,
            Dictionary<(Guid campusId, string code), DatabaseBuilding> buildings,
            Dictionary<string, DatabaseInstructor> instructors,
            Dictionary<(string number, string title), DatabaseCourse> courses,
            Dictionary<string, DatabaseSection> sections,
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
            DatabaseCampus campus = FetchOrAddCampus(campuses, campusCode, campusName);

            // Hydrate course
            var sectionWithCourse = sectionGroup
                .FirstOrDefault(s => 
                    (s.CourseNumber.Length > 0) && 
                    (s.CourseTitle.Length > 0));
            if (sectionWithCourse == null)
            {
                Console.Error.WriteLine("WARNING: No course information found for CRNs " + 
                    $"{string.Join(", ", sectionGroup.Select(s => s.Crn))}");
                return;
            }
            var creditHours = sectionGroup
                .OrderByDescending(c => c.CreditHours)
                .FirstOrDefault()
                .CreditHours;
            DatabaseCourse course = FetchOrAddCourse(subject, courses,
                sectionWithCourse.CourseNumber, sectionWithCourse.CourseTitle, creditHours,
                sectionWithCourse.Description);

            // Hydrate class
            Guid classId = Guid.Empty;
            var sectionWithClass = sectionGroup.FirstOrDefault(s => sections.ContainsKey(s.Crn));
            if (sectionWithClass == null)
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
                classId = sections[sectionWithClass.Crn].ClassId;
            }

            // Hydrate each section
            foreach (var section in sectionGroup)
            {
                var dbSection = AddOrUpdateSection(sections, classId, section);
                AddOrUpdateSectionMeetings(campus, buildings, instructors, dbSection, section);
            }
        }

        private DatabaseCampus FetchOrAddCampus(Dictionary<string, DatabaseCampus> campuses,
            string campusCode, string campusName)
        {
            DatabaseCampus campus;
            if (campuses.ContainsKey(campusCode))
            {
                campus = campuses[campusCode];
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
                campuses[campusCode] = campus;
            }
            return campus;
        }

        private DatabaseCourse FetchOrAddCourse(
            DatabaseSubject subject,
            Dictionary<(string number, string title), DatabaseCourse> courses,
            string courseNumber, string courseTitle, double creditHours, string courseDescription)
        {
            DatabaseCourse course;
            var courseKey = (number: courseNumber, title: courseTitle);
            if (courses.ContainsKey(courseKey))
            {
                course = courses[courseKey];
            }
            else
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
                courses[courseKey] = course;
            }
            return course;
        }

        private DatabaseSection AddOrUpdateSection(Dictionary<string, DatabaseSection> sections,
            Guid classId, ScrapedSection section)
        {
            var startDate = section.Meetings
                .OrderBy(m => m.StartDate)
                .Select(m => m.StartDate)
                .First();

            var endDate = section.Meetings
                .OrderByDescending(m => m.EndDate)
                .Select(m => m.EndDate)
                .First();

            if (sections.ContainsKey(section.Crn))
            {
                var existingSection = sections[section.Crn];
                var dbEntry = dbContext.Entry(existingSection);
                if (existingSection.ClassId != classId)
                {
                    existingSection.ClassId = classId;
                    dbEntry.Property(s => s.ClassId).CurrentValue = classId;
                }
                if (existingSection.Type != section.Type)
                {
                    existingSection.Type = section.Type;
                    dbEntry.Property(s => s.Type).CurrentValue = section.Type;
                }
                // TODO: Registration status..?
                if (existingSection.StartDate != startDate)
                {
                    existingSection.StartDate = startDate;
                    dbEntry.Property(s => s.StartDate).CurrentValue = startDate;
                }
                if (existingSection.EndDate != endDate)
                {
                    existingSection.EndDate = endDate;
                    dbEntry.Property(s => s.EndDate).CurrentValue = endDate;
                }
                if (existingSection.Capacity != section.Capacity)
                {
                    existingSection.Capacity = section.Capacity;
                    dbEntry.Property(s => s.Capacity).CurrentValue = section.Capacity;
                }
                if (existingSection.Enrolled != section.Enrolled)
                {
                    existingSection.Enrolled = section.Enrolled;
                    dbEntry.Property(s => s.Enrolled).CurrentValue = section.Enrolled;
                }
                if (existingSection.RemainingSpace != section.RemainingSpace)
                {
                    existingSection.RemainingSpace = section.RemainingSpace;
                    dbEntry.Property(s => s.RemainingSpace).CurrentValue = 
                        section.RemainingSpace;
                }
                if (existingSection.WaitListCapacity != section.WaitListCapacity)
                {
                    existingSection.WaitListCapacity = section.WaitListCapacity;
                    dbEntry.Property(s => s.WaitListCapacity).CurrentValue =
                        section.WaitListCapacity;
                }
                if (existingSection.WaitListCount != section.WaitListCount)
                {
                    existingSection.WaitListCount = section.WaitListCount;
                    dbEntry.Property(s => s.WaitListCount).CurrentValue = section.WaitListCount;
                }
                if (existingSection.WaitListSpace != section.WaitListSpace)
                {
                    existingSection.WaitListSpace = section.WaitListSpace;
                    dbEntry.Property(s => s.WaitListSpace).CurrentValue = section.WaitListSpace;
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
                    RegistrationStatus = RegistrationStatus.NotAvailable,
                    StartDate = startDate,
                    EndDate = endDate,
                    Capacity = section.Capacity,
                    Enrolled = section.Enrolled,
                    RemainingSpace = section.RemainingSpace,
                    WaitListCapacity = section.WaitListCapacity,
                    WaitListCount = section.WaitListCount,
                    WaitListSpace = section.WaitListSpace,
                };
                dbContext.Add(newSection);
                return newSection;
            }
        }

        private void AddOrUpdateSectionMeetings(
            DatabaseCampus campus,
            Dictionary<(Guid campusId, string code), DatabaseBuilding> buildings,
            Dictionary<string, DatabaseInstructor> instructors,
            DatabaseSection dbSection, ScrapedSection section)
        {
            var foundMeetingIds = new List<Guid>();
            foreach (var meeting in section.Meetings)
            {
                var meetingDuration = meeting.EndTime.Subtract(meeting.StartTime);

                // MyPurdue doesn't expose any sort of unique ID for each meeting,
                // so we need to determine equality via a heuristic:
                // If it happens at the same time at the same location, it is the same
                // meeting.
                var dbMeeting = dbSection.Meetings?.FirstOrDefault(m => 
                    (m.Room?.Number == meeting.RoomNumber) &&
                    (m.Room?.Building?.ShortCode == meeting.BuildingCode) &&
                    (m.DaysOfWeek == (DatabaseDaysOfWeek) meeting.DaysOfWeek) &&
                    (m.StartDate.Subtract(meeting.StartDate).Duration()
                        <= MEETING_TIME_EQUALITY_TOLERANCE) &&
                    (m.EndDate.Subtract(meeting.EndDate).Duration()
                        <= MEETING_TIME_EQUALITY_TOLERANCE) &&
                    (m.StartTime.Subtract(meeting.StartTime).Duration()
                        <= MEETING_TIME_EQUALITY_TOLERANCE) &&
                    (m.Duration.Subtract(meetingDuration).Duration()
                        <= MEETING_TIME_EQUALITY_TOLERANCE));

                var dbRoom = FetchOrAddMeetingRoom(campus, buildings, dbSection, meeting);
                var dbInstructors = FetchOrAddMeetingInstructors(instructors, meeting);

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

                if (dbMeeting.Instructors != null)
                {
                    var instructorsToRemove = dbMeeting.Instructors.Where(i => 
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
                }
                foreach (var dbInstructor in dbInstructors)
                {
                    if (dbMeeting.Instructors.SingleOrDefault(i => 
                        (i.Id == dbInstructor.Id)) == null)
                    {
                        dbMeeting.Instructors.Add(dbInstructor);
                        dbContext.Add(new MeetingInstructor()
                        {
                            MeetingId = dbMeeting.Id,
                            InstructorId = dbInstructor.Id,
                        });
                    }
                }
            }
        }

        private DatabaseRoom FetchOrAddMeetingRoom(
            DatabaseCampus campus,
            Dictionary<(Guid campusId, string code), DatabaseBuilding> buildings,
            DatabaseSection dbSection, ScrapedMeeting meeting)
        {
            var dbBuilding = FetchOrAddMeetingBuilding(campus, buildings, dbSection, meeting);
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

        private DatabaseBuilding FetchOrAddMeetingBuilding(
            DatabaseCampus campus,
            Dictionary<(Guid campusId, string code), DatabaseBuilding> buildings,
            DatabaseSection dbSection, ScrapedMeeting meeting)
        {
            var buildingKey = (campusId: campus.Id, code: meeting.BuildingCode);
            if (buildings.ContainsKey(buildingKey))
            {
                return buildings[buildingKey];
            }
            else
            {
                var dbBuilding = new DatabaseBuilding()
                {
                    Id = Guid.NewGuid(),
                    CampusId = campus.Id,
                    Name = meeting.BuildingName,
                    ShortCode = meeting.BuildingCode,
                    Rooms = new List<DatabaseRoom>(),
                };
                dbContext.Add(dbBuilding);
                buildings[buildingKey] = dbBuilding;
                return dbBuilding;
            }
        }

        private ICollection<DatabaseInstructor> FetchOrAddMeetingInstructors(
            Dictionary<string, DatabaseInstructor> instructors, ScrapedMeeting meeting)
        {
            var returnVal = new List<DatabaseInstructor>();
            foreach (var scrapedInstructor in meeting.Instructors)
            {
                if (instructors.ContainsKey(scrapedInstructor.email))
                {
                    returnVal.Add(instructors[scrapedInstructor.email]);
                }
                else
                {
                    var dbInstructor = new DatabaseInstructor()
                    {
                        Id = Guid.NewGuid(),
                        Name = scrapedInstructor.name,
                        Email = scrapedInstructor.email,
                    };
                    dbContext.Add(dbInstructor);
                    instructors[scrapedInstructor.email] = dbInstructor;
                    returnVal.Add(dbInstructor);
                }
            }
            return returnVal;
        }
    }
}