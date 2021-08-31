using PurdueIo.CatalogSync.Tests.Mocks;
using PurdueIo.Database;
using PurdueIo.Scraper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using DaysOfWeek = PurdueIo.Scraper.Models.DaysOfWeek;
using ScrapedMeeting = PurdueIo.Scraper.Models.Meeting;
using ScrapedSection = PurdueIo.Scraper.Models.Section;
using ScrapedSubject = PurdueIo.Scraper.Models.Subject;
using ScrapedTerm = PurdueIo.Scraper.Models.Term;

namespace PurdueIo.CatalogSync.Tests
{
    public class SynchronizerTests
    {
        [Fact]
        public async Task BasicSynchronizationTest()
        {
            var termCode = "202210";
            var termName = "Fall 2021";
            var terms = new List<ScrapedTerm>()
            {
                new ScrapedTerm()
                {
                    Id = termCode,
                    Name = termName,
                },
            };

            var subjectCode = "TEST";
            var subjectName = "Test Subject";
            var subjects = new List<ScrapedSubject>()
            {
                new ScrapedSubject()
                {
                    Code = subjectCode,
                    Name = subjectName,
                },
            };

            var courseNumber = "10100";
            var courseName = "Intro to Test";
            var courseDescription = "How to test things";
            var courseCreditHours = 1.0d;
            var campusName = "Test Campus";
            var campusCode = "TC";
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var firstBuilding = (code: "TB", name: "Test Building");
            var secondBuilding = (code: "OB", name: "Other Building");
            var firstRoomNumber = "543";
            var secondRoomNumber = "321";
            var thirdRoomNumber = "111";
            var sections = new List<ScrapedSection>()
            {
                new ScrapedSection()
                {
                    Crn = "12345",
                    SectionCode = "000",
                    Meetings = new ScrapedMeeting[]
                    {
                        new ScrapedMeeting()
                        {
                            Type = "Lecture",
                            Instructors = new (string name, string email)[]
                            {
                                (name: "Hayden McAfee", email: "haydenmc@test.com"),
                            },
                            StartDate = new DateTimeOffset(2021, 8, 23, 0, 0, 0, 0,
                                timeZone.BaseUtcOffset),
                            EndDate = new DateTimeOffset(2021, 12, 11, 0, 0, 0, 0,
                                timeZone.BaseUtcOffset),
                            DaysOfWeek =
                                (DaysOfWeek.Monday | DaysOfWeek.Wednesday),
                            StartTime = new DateTimeOffset(2021, 8, 30, 7, 30, 0, 0,
                                timeZone.BaseUtcOffset),
                            EndTime = new DateTimeOffset(2021, 8, 30, 8, 20, 0, 0,
                                timeZone.BaseUtcOffset),
                            BuildingCode = firstBuilding.code,
                            BuildingName = firstBuilding.name,
                            RoomNumber = firstRoomNumber,
                        },
                    },
                    SubjectCode = subjectCode,
                    CourseNumber = courseNumber,
                    Type = "Lecture",
                    CourseTitle = courseName,
                    Description = courseDescription,
                    CreditHours = courseCreditHours,
                    LinkSelf = "A0",
                    LinkOther = "A2",
                    CampusCode = campusCode,
                    CampusName = campusName,
                    Capacity = 32,
                    Enrolled = 16,
                    RemainingSpace = 16,
                    WaitListCapacity = 8,
                    WaitListCount = 4,
                    WaitListSpace = 4,
                },
                new ScrapedSection()
                {
                    Crn = "12346",
                    SectionCode = "001",
                    Meetings = new ScrapedMeeting[]
                    {
                        new ScrapedMeeting()
                        {
                            Type = "Recitation",
                            Instructors = new (string name, string email)[]
                            {
                                (name: "Mayden HcAfee", email: "maydenhc@test.com"),
                            },
                            StartDate = new DateTimeOffset(2021, 8, 23, 0, 0, 0, 0,
                                timeZone.BaseUtcOffset),
                            EndDate = new DateTimeOffset(2021, 12, 11, 0, 0, 0, 0,
                                timeZone.BaseUtcOffset),
                            DaysOfWeek =
                                (DaysOfWeek.Tuesday | DaysOfWeek.Thursday),
                            StartTime = new DateTimeOffset(2021, 8, 30, 12, 30, 0, 0,
                                timeZone.BaseUtcOffset),
                            EndTime = new DateTimeOffset(2021, 8, 30, 13, 20, 0, 0,
                                timeZone.BaseUtcOffset),
                            BuildingCode = secondBuilding.code,
                            BuildingName = secondBuilding.name,
                            RoomNumber = secondRoomNumber,
                        },
                    },
                    SubjectCode = subjectCode,
                    CourseNumber = courseNumber,
                    Type = "Recitation",
                    CourseTitle = courseName,
                    Description = courseDescription,
                    CreditHours = courseCreditHours,
                    LinkSelf = "A2",
                    LinkOther = "A1",
                    CampusCode = campusCode,
                    CampusName = campusName,
                    Capacity = 16,
                    Enrolled = 8,
                    RemainingSpace = 8,
                    WaitListCapacity = 4,
                    WaitListCount = 2,
                    WaitListSpace = 2,
                },
                new ScrapedSection()
                {
                    Crn = "12347",
                    SectionCode = "002",
                    Meetings = new ScrapedMeeting[]
                    {
                        new ScrapedMeeting()
                        {
                            Type = "Laboratory",
                            Instructors = new (string name, string email)[]
                            {
                                (name: "Mayden HcAfee", email: "maydenhc@test.com"),
                            },
                            StartDate = new DateTimeOffset(2021, 8, 23, 0, 0, 0, 0,
                                timeZone.BaseUtcOffset),
                            EndDate = new DateTimeOffset(2021, 12, 11, 0, 0, 0, 0,
                                timeZone.BaseUtcOffset),
                            DaysOfWeek = DaysOfWeek.Friday,
                            StartTime = new DateTimeOffset(2021, 8, 30, 14, 30, 0, 0,
                                timeZone.BaseUtcOffset),
                            EndTime = new DateTimeOffset(2021, 8, 30, 16, 20, 0, 0,
                                timeZone.BaseUtcOffset),
                            BuildingCode = secondBuilding.code,
                            BuildingName = secondBuilding.name,
                            RoomNumber = thirdRoomNumber,
                        },
                    },
                    SubjectCode = subjectCode,
                    CourseNumber = courseNumber,
                    Type = "Laboratory",
                    CourseTitle = courseName,
                    Description = courseDescription,
                    CreditHours = courseCreditHours,
                    LinkSelf = "A1",
                    LinkOther = "A0",
                    CampusCode = campusCode,
                    CampusName = campusName,
                    Capacity = 8,
                    Enrolled = 4,
                    RemainingSpace = 4,
                    WaitListCapacity = 2,
                    WaitListCount = 1,
                    WaitListSpace = 1,
                }
            };
            
            IScraper scraper = new MockScraper(terms, subjects, sections);
            var dbContext = new ApplicationDbContext(Path.GetTempFileName());
            await Synchronizer.SynchronizeAsync(scraper, dbContext);

            var dbTerm = dbContext.Terms.SingleOrDefault(t => 
                (t.Code == termCode) &&
                (t.Name == termName));
            Assert.NotNull(dbTerm);

            var dbSubject = dbContext.Subjects.SingleOrDefault(s => 
                (s.Abbreviation == subjectCode) &&
                (s.Name == subjectName));
            Assert.NotNull(dbSubject);

            var dbCourse = dbContext.Courses.SingleOrDefault(c => 
                (c.Subject.Abbreviation == subjectCode) &&
                (c.Subject.Name == subjectName) &&
                (c.Number == courseNumber) && 
                (c.Title == courseName) &&
                (c.CreditHours == courseCreditHours) &&
                (c.Description == courseDescription));
            Assert.NotNull(dbCourse);

            var dbCampus = dbContext.Campuses.SingleOrDefault(c =>
                (c.Code == campusCode) &&
                (c.Name == campusName));
            Assert.NotNull(dbCampus);

            var dbClass = dbContext.Classes.SingleOrDefault(c =>
                (c.CourseId == dbCourse.Id) &&
                (c.TermId == dbTerm.Id) &&
                (c.CampusId == dbCampus.Id));
            Assert.NotNull(dbClass);

            var dbFirstBuilding = dbContext.Buildings.SingleOrDefault(b => 
                (b.ShortCode == firstBuilding.code) &&
                (b.Name == firstBuilding.name));
            Assert.NotNull(dbFirstBuilding);
            var dbSecondBuilding = dbContext.Buildings.SingleOrDefault(b => 
                (b.ShortCode == secondBuilding.code) &&
                (b.Name == secondBuilding.name));
            Assert.NotNull(dbSecondBuilding);

            var dbFirstRoom = dbContext.Rooms.SingleOrDefault(r =>
                (r.BuildingId == dbFirstBuilding.Id) &&
                (r.Number == firstRoomNumber));
            Assert.NotNull(dbFirstRoom);
            var dbSecondRoom = dbContext.Rooms.SingleOrDefault(r =>
                (r.BuildingId == dbSecondBuilding.Id) &&
                (r.Number == secondRoomNumber));
            Assert.NotNull(dbSecondRoom);
            var dbThirdRoom = dbContext.Rooms.SingleOrDefault(r =>
                (r.BuildingId == dbSecondBuilding.Id) &&
                (r.Number == thirdRoomNumber));
            Assert.NotNull(dbThirdRoom);

            var lectureSection = dbContext.Sections.SingleOrDefault(s =>
                (s.ClassId == dbClass.Id) &&
                (s.Type == "Lecture"));
            Assert.NotNull(lectureSection);
            var recitationSection = dbContext.Sections.SingleOrDefault(s =>
                (s.ClassId == dbClass.Id) &&
                (s.Type == "Recitation"));
            Assert.NotNull(recitationSection);
            var labSection = dbContext.Sections.SingleOrDefault(s =>
                (s.ClassId == dbClass.Id) &&
                (s.Type == "Laboratory"));
            Assert.NotNull(labSection);

            var lectureMeeting = dbContext.Meetings.SingleOrDefault(m =>
                (m.SectionId == lectureSection.Id) && 
                (m.Type == "Lecture") &&
                (m.RoomId == dbFirstRoom.Id));
            Assert.NotNull(lectureMeeting);
            var recitationMeeting = dbContext.Meetings.SingleOrDefault(m =>
                (m.SectionId == recitationSection.Id) && 
                (m.Type == "Recitation") &&
                (m.RoomId == dbSecondRoom.Id));
            Assert.NotNull(recitationMeeting);
            var labMeeting = dbContext.Meetings.SingleOrDefault(m =>
                (m.SectionId == labSection.Id) && 
                (m.Type == "Laboratory") &&
                (m.RoomId == dbThirdRoom.Id));
            Assert.NotNull(labMeeting);
        }
    }
}