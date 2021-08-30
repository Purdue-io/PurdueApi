using Microsoft.EntityFrameworkCore;
using PurdueIo.Database;
using PurdueIo.Database.Models;
using PurdueIo.Scraper;
using PurdueIo.Scraper.Models;
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
using ScrapedSubject = PurdueIo.Scraper.Models.Subject;
using ScrapedSection = PurdueIo.Scraper.Models.Section;
using ScrapedTerm = PurdueIo.Scraper.Models.Term;
using ScrapedMeeting = PurdueIo.Scraper.Models.Meeting;

namespace PurdueIo.CatalogSync
{
    public class Synchronizer
    {
        public static Task SynchronizeAsync(IScraper scraper, ApplicationDbContext dbContext)
        {
            return new Synchronizer(scraper, dbContext).SynchronizeInternalAsync();
        }

        private readonly TimeSpan MEETING_TIME_EQUALITY_TOLERANCE = TimeSpan.FromMinutes(1);

        private IScraper scraper;

        private ApplicationDbContext dbContext;

        private Synchronizer(IScraper scraper, ApplicationDbContext dbContext)
        {
            this.scraper = scraper;
            this.dbContext = dbContext;
        }

        private async Task SynchronizeInternalAsync()
        {
            var terms = await scraper.GetTermsAsync();
            foreach (var term in terms)
            {
                await SynchronizeTermInternalAsync(term);
            }
        }

        private async Task SynchronizeTermInternalAsync(ScrapedTerm scrapedTerm)
        {
            // Check the local EF view for newly added but un-saved terms before
            // querying the real database.
            var dbTerm =
                dbContext.Terms.Local
                    .SingleOrDefault(t => t.Code == scrapedTerm.Id) ??
                dbContext.Terms
                    .SingleOrDefault(t => t.Code == scrapedTerm.Id);
            if (dbTerm == null)
            {
                dbTerm = new DatabaseTerm()
                {
                    Id = Guid.NewGuid(),
                    Code = scrapedTerm.Id,
                    Name = scrapedTerm.Name,
                    StartDate = DateTimeOffset.MinValue,
                    EndDate = DateTimeOffset.MaxValue,
                    Classes = new List<DatabaseClass>(),
                };
                dbContext.Terms.Add(dbTerm);
            }

            var subjects = await scraper.GetSubjectsAsync(dbTerm.Code);
            foreach (var subject in subjects)
            {
                await SynchronizeTermSubjectAsync(dbTerm, subject);
            }
        }
        
        private async Task SynchronizeTermSubjectAsync(DatabaseTerm dbTerm,
            ScrapedSubject scrapedSubject)
        {
            var dbSubject = HydrateSubject(scrapedSubject.Code, scrapedSubject.Name);

            ICollection<ScrapedSection> scrapedSections =
                await scraper.GetSectionsAsync(dbTerm.Code, scrapedSubject.Code);

            ICollection<ICollection<ScrapedSection>> groupedSections = 
                SectionLinker.GroupLinkedSections(scrapedSections);

            foreach (var sectionGroup in groupedSections)
            {
                var sectionWithCourseInfo = sectionGroup
                    .FirstOrDefault(s => (s.CourseNumber.Length > 0) && (s.CourseTitle.Length > 0));
                if (sectionWithCourseInfo == null)
                {
                    Console.WriteLine("WARNING: No course information found for CRNs " + 
                        $"{string.Join(", ", sectionGroup.Select(s => s.Crn))}");
                    continue;
                }

                Console.WriteLine($"{sectionWithCourseInfo.SubjectCode}" +
                    $"{sectionWithCourseInfo.CourseNumber} - " +
                    $"{sectionWithCourseInfo.CourseTitle}...");

                var sectionWithCampus = 
                    sectionGroup.FirstOrDefault(s => s.CampusCode.Length > 0);
                DatabaseCampus dbCampus;
                if (sectionWithCampus == null)
                {
                    dbCampus = HydrateCampus("", "");
                }
                else
                {
                    dbCampus = HydrateCampus(sectionWithCampus.CampusCode,
                        sectionWithCampus.CampusName);
                }

                var dbCourse = HydrateCourse(dbSubject, sectionWithCourseInfo, sectionGroup);

                var dbClass = HydrateClass(dbCampus, dbCourse, dbTerm, sectionGroup);

                foreach (var section in sectionGroup)
                {
                    UpdateSection(section, dbClass);
                }
            }

            dbContext.SaveChanges();
        }

        private DatabaseSubject HydrateSubject(string subjectAbbreviation, string subjectName)
        {
            // Check the local EF view for newly added but un-saved subjects before
            // querying the real database.
            var dbSubject = 
                dbContext.Subjects.Local
                    .SingleOrDefault(s => s.Abbreviation.Equals(subjectAbbreviation,
                        StringComparison.OrdinalIgnoreCase)) ??
                dbContext.Subjects
                    .SingleOrDefault(s => EF.Functions.Like(s.Abbreviation, subjectAbbreviation));
            if (dbSubject == null)
            {
                dbSubject = new DatabaseSubject()
                {
                    Id = Guid.NewGuid(),
                    Abbreviation = subjectAbbreviation,
                    Name = subjectName,
                };
                dbContext.Subjects.Add(dbSubject);
            }
            return dbSubject;
        }

        private DatabaseCampus HydrateCampus(string campusCode, string campusName)
        {
            // Check the local EF view for newly added but un-saved campuses before
            // querying the real database.
            var dbCampus =
                dbContext.Campuses.Local
                    .SingleOrDefault(c => 
                        c.Code.Equals(campusCode, StringComparison.OrdinalIgnoreCase) && 
                        c.Name.Equals(campusName, StringComparison.OrdinalIgnoreCase)) ??
                dbContext.Campuses
                    .SingleOrDefault(c => 
                        EF.Functions.Like(c.Code, campusCode) &&
                        EF.Functions.Like(c.Name, campusName));
            if (dbCampus == null)
            {
                dbCampus = new Campus()
                {
                    Id = Guid.NewGuid(),
                    Code = campusCode,
                    Name = campusName
                };
                dbContext.Campuses.Add(dbCampus);
            }
            return dbCampus;
        }

        private DatabaseCourse HydrateCourse(DatabaseSubject dbSubject, ScrapedSection sectionWithCourseInfo,
            ICollection<ScrapedSection> sectionGroup)
        {
            // Check the local EF view for newly added but un-saved courses before
            // querying the real database.
            var dbCourse = 
                dbContext.Courses.Local.SingleOrDefault(c =>
                    (c.SubjectId == dbSubject.Id) &&
                    c.Number.Equals(sectionWithCourseInfo.CourseNumber,
                        StringComparison.OrdinalIgnoreCase) && 
                    c.Title.Equals(sectionWithCourseInfo.CourseTitle,
                        StringComparison.OrdinalIgnoreCase)) ??
                dbContext.Courses.SingleOrDefault(c =>
                    (c.SubjectId == dbSubject.Id) &&
                    EF.Functions.Like(c.Number, sectionWithCourseInfo.CourseNumber) && 
                    EF.Functions.Like(c.Title, sectionWithCourseInfo.CourseTitle));
            if (dbCourse == null)
            {
                dbCourse = new Course()
                {
                    Id = Guid.NewGuid(),
                    Number = sectionWithCourseInfo.CourseNumber,
                    SubjectId = dbSubject.Id,
                    Title = sectionWithCourseInfo.CourseTitle,
                    CreditHours = sectionGroup
                        .OrderByDescending(c => c.CreditHours)
                        .FirstOrDefault()
                        .CreditHours,
                    Description = sectionWithCourseInfo.Description,
                };
                dbContext.Courses.Add(dbCourse);
            }
            return dbCourse;
        }

        private DatabaseClass HydrateClass(DatabaseCampus dbCampus, DatabaseCourse dbCourse,
            DatabaseTerm dbTerm, ICollection<ScrapedSection> sectionGroup)
        {
            var crns = sectionGroup.Select(s => s.Crn).ToList();
            // Check the local EF view for newly added but un-saved classes before
            // querying the real database.
            var dbClass = 
                dbContext.Sections.Local
                    .Where(s => crns.Contains(s.Crn))
                    .Select(s => s.Class)
                    .FirstOrDefault() ??
                dbContext.Sections
                    .Where(s => crns.Contains(s.Crn))
                    .Include(c => c.Class.Campus)
                    .Select(s => s.Class)
                    .FirstOrDefault();

            if (dbClass == null)
            {
                dbClass = new DatabaseClass()
                {
                    Id = Guid.NewGuid(),
                    CourseId = dbCourse.Id,
                    TermId = dbTerm.Id,
                    CampusId = dbCampus.Id,
                    Campus = dbCampus,
                    Sections = new List<DatabaseSection>(),
                };
                dbContext.Classes.Add(dbClass);
            }

            return dbClass;
        }

        private void UpdateSection(ScrapedSection section, DatabaseClass dbClass)
        {
            // Check the local EF view for newly added but un-saved classes before
            // querying the real database.
            // We don't need to include related entities for local sections, since they are
            // tracked on the client until changes are saved.
            var dbSection = 
                dbContext.Sections.Local
                    .SingleOrDefault(c => c.Crn == section.Crn) ??
                dbContext.Sections
                    .Include(s => s.Meetings)
                    .ThenInclude(m => m.Room)
                    .ThenInclude(m => m.Building)
                    .SingleOrDefault(c => c.Crn == section.Crn);

            var startDate = section.Meetings
                .OrderBy(m => m.StartDate)
                .Select(m => m.StartDate)
                .First();

            var endDate = section.Meetings
                .OrderByDescending(m => m.EndDate)
                .Select(m => m.EndDate)
                .First();

            // If still null, we've never seen this section before.
            // Create a new one.
            if (dbSection == null)
            {
                dbSection = new DatabaseSection()
                {
                    Id = Guid.NewGuid(),
                    Crn = section.Crn,
                    ClassId = dbClass.Id,
                    // Class = dbClass,
                    Meetings = new List<DatabaseMeeting>(),
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
                dbContext.Sections.Add(dbSection);
            }
            else
            {
                if (dbSection.ClassId != dbClass.Id)
                {
                    dbSection.ClassId = dbClass.Id;
                }
                if (dbSection.Type != section.Type)
                {
                    dbSection.Type = section.Type;
                }
                // TODO: Registration status..?
                if (dbSection.StartDate != startDate)
                {
                    dbSection.StartDate = startDate;
                }
                if (dbSection.EndDate != endDate)
                {
                    dbSection.EndDate = endDate;
                }
                if (dbSection.Capacity != section.Capacity)
                {
                    dbSection.Capacity = section.Capacity;
                }
                if (dbSection.Enrolled != section.Enrolled)
                {
                    dbSection.Enrolled = section.Enrolled;
                }
                if (dbSection.RemainingSpace != section.RemainingSpace)
                {
                    dbSection.RemainingSpace = section.RemainingSpace;
                }
                if (dbSection.WaitListCapacity != section.WaitListCapacity)
                {
                    dbSection.WaitListCapacity = section.WaitListCapacity;
                }
                if (dbSection.WaitListCount != section.WaitListCount)
                {
                    dbSection.WaitListCount = section.WaitListCount;
                }
                if (dbSection.WaitListSpace != section.WaitListSpace)
                {
                    dbSection.WaitListSpace = section.WaitListSpace;
                }
            }

            // Update meetings for this section
            UpdateSectionMeetings(dbSection, section);
        }

        private void UpdateSectionMeetings(DatabaseSection dbSection, ScrapedSection section)
        {
            var foundMeetingIds = new List<Guid>();
            foreach (var meeting in section.Meetings)
            {
                var meetingDuration = meeting.EndTime.Subtract(meeting.StartTime);

                // MyPurdue doesn't expose any sort of unique ID for each meeting,
                // so we need to determine equality via a heuristic:
                // If it happens at the same time at the same location, it is the same
                // meeting.
                //
                // Since a Meeting is n:1 with a Section and not shared with any other entities,
                // we don't need to check for any unsaved local meetings.
                var dbMeeting = dbSection.Meetings.FirstOrDefault(m => 
                    (m.Room.Number == meeting.RoomNumber) &&
                    (m.Room.Building.ShortCode == meeting.BuildingCode) &&
                    (m.DaysOfWeek == (DatabaseDaysOfWeek) meeting.DaysOfWeek) &&
                    (m.StartDate.Subtract(meeting.StartDate).Duration()
                        <= MEETING_TIME_EQUALITY_TOLERANCE) &&
                    (m.EndDate.Subtract(meeting.EndDate).Duration()
                        <= MEETING_TIME_EQUALITY_TOLERANCE) &&
                    (m.StartTime.Subtract(meeting.StartTime).Duration()
                        <= MEETING_TIME_EQUALITY_TOLERANCE) &&
                    (m.Duration.Subtract(meetingDuration).Duration()
                        <= MEETING_TIME_EQUALITY_TOLERANCE));

                var dbRoom = UpdateMeetingRoom(dbSection, meeting);

                if (dbMeeting == null)
                {
                    dbMeeting = new DatabaseMeeting()
                    {
                        Id = Guid.NewGuid(),
                        SectionId = dbSection.Id,
                        // Section = dbSection,
                        Type = meeting.Type,
                        Instructors = new List<DatabaseInstructor>(),
                        StartDate = meeting.StartDate,
                        EndDate = meeting.EndDate,
                        DaysOfWeek = (DatabaseDaysOfWeek) meeting.DaysOfWeek,
                        StartTime = meeting.StartTime,
                        Duration = meetingDuration,
                        RoomId = dbRoom.Id,
                        // Room = dbRoom,
                    };
                    dbContext.Meetings.Add(dbMeeting);
                }
                foundMeetingIds.Add(dbMeeting.Id);
            }
            
            // Remove any outstanding meetings that did not match the scraped meeting information
            dbContext.Meetings.RemoveRange(dbContext.Meetings.Where(m =>
                (m.SectionId == dbSection.Id) && (!foundMeetingIds.Contains(m.Id))));
        }

        private DatabaseRoom UpdateMeetingRoom(DatabaseSection dbSection,
            ScrapedMeeting scrapedMeeting)
        {
            // Check the local EF view for newly added but un-saved rooms before
            // querying the real database.
            var dbRoom =
                dbContext.Rooms.Local.FirstOrDefault(r => 
                    (r.Building.Campus.Id == dbSection.Class.CampusId) &&
                    (r.Building.ShortCode == scrapedMeeting.BuildingCode) &&
                    (r.Number == scrapedMeeting.RoomNumber)) ?? 
                dbContext.Rooms.FirstOrDefault(r => 
                    (r.Building.Campus.Id == dbSection.Class.CampusId) &&
                    (r.Building.ShortCode == scrapedMeeting.BuildingCode) &&
                    (r.Number == scrapedMeeting.RoomNumber));
            if (dbRoom == null)
            {
                var dbBuilding = UpdateMeetingBuilding(dbSection, scrapedMeeting);
                dbRoom = new DatabaseRoom()
                {
                    Id = Guid.NewGuid(),
                    Number = scrapedMeeting.RoomNumber,
                    BuildingId = dbBuilding.Id,
                    //Building = dbBuilding,
                };
                dbContext.Rooms.Add(dbRoom); // Don't need this since it's referenced by the meeting?
            }
            return dbRoom;
        }

        private DatabaseBuilding UpdateMeetingBuilding(DatabaseSection dbSection,
            ScrapedMeeting scrapedMeeting)
        {
            // Check the local EF view for newly added but un-saved buildings before
            // querying the real database.
            var dbBuilding =
                dbContext.Buildings.Local.FirstOrDefault(b =>
                    (b.CampusId == dbSection.Class.CampusId) &&
                    (b.ShortCode == scrapedMeeting.BuildingCode)) ??
                dbContext.Buildings.FirstOrDefault(b =>
                    (b.CampusId == dbSection.Class.CampusId) &&
                    (b.ShortCode == scrapedMeeting.BuildingCode));
            if (dbBuilding == null)
            {
                dbBuilding = new DatabaseBuilding()
                {
                    Id = Guid.NewGuid(),
                    CampusId = dbSection.Class.CampusId,
                    //Campus = dbSection.Class.Campus,
                    Name = scrapedMeeting.BuildingName,
                    ShortCode = scrapedMeeting.BuildingCode,
                    Rooms = new List<DatabaseRoom>(),
                };
                dbContext.Buildings.Add(dbBuilding); // Don't need this since it's referenced by the room?
            }
            return dbBuilding;
        }
    }
}