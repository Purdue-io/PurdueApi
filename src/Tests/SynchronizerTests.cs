using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PurdueIo.CatalogSync;
using PurdueIo.Database;
using PurdueIo.Scraper;
using PurdueIo.Tests.Mocks;
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

namespace PurdueIo.Tests
{
    public class SynchronizerTests
    {
        [Fact]
        public async Task BasicSectionSyncTest()
        {
            using (var dbContext = GetDbContext())
            {
                var testSection = GenerateSection();
                var testMeeting = testSection.Section.Meetings.FirstOrDefault();
                var scraper = GetScraper(testSection.Section);
                var term = (await scraper.GetTermsAsync()).FirstOrDefault();
                var subject = (await scraper.GetSubjectsAsync(term.Id)).FirstOrDefault();
                await Synchronizer.SynchronizeAsync(scraper, dbContext);

                // Ensure the section and all related entities are properly persisted in the DB
                // Term
                Assert.NotNull(dbContext.Terms.SingleOrDefault(t => t.Code == term.Id));
                Assert.Equal(testMeeting.StartDate,
                    dbContext.Terms.SingleOrDefault(t => t.Code == term.Id).StartDate);
                Assert.Equal(testMeeting.EndDate,
                    dbContext.Terms.SingleOrDefault(t => t.Code == term.Id).EndDate);
                // Subject
                Assert.NotNull(dbContext.Subjects.SingleOrDefault(s => 
                    (s.Abbreviation == subject.Code) && (s.Name == subject.Name)));
                // Course
                Assert.NotNull(dbContext.Courses.SingleOrDefault(c =>
                    (c.Number == testSection.Course.Number) &&
                    (c.Title == testSection.Course.Name) &&
                    (c.CreditHours == testSection.Course.CreditHours) &&
                    (c.Description == testSection.Course.Description) &&
                    (c.Subject.Abbreviation == subject.Code)));
                // Campus
                Assert.NotNull(dbContext.Campuses.SingleOrDefault(c => 
                    (c.Code == testSection.Campus.Code) &&
                    (c.Name == testSection.Campus.Name)));
                // Class
                Assert.NotNull(dbContext.Classes.SingleOrDefault(c => 
                    (c.Term.Code == term.Id) &&
                    (c.Campus.Code == testSection.Campus.Code) &&
                    (c.Course.Number == testSection.Course.Number) &&
                    (c.Course.Title == testSection.Course.Name) &&
                    (c.Course.Subject.Abbreviation == subject.Code)));
                // Building
                Assert.NotNull(dbContext.Buildings.SingleOrDefault(b =>
                    (b.Name == testSection.Room.Building.Name) &&
                    (b.ShortCode == testSection.Room.Building.Code)));
                // Room
                Assert.NotNull(dbContext.Rooms.SingleOrDefault(r =>
                    (r.Building.ShortCode == testSection.Room.Building.Code) &&
                    (r.Number == testSection.Room.Number)));
                // Section
                Assert.NotNull(dbContext.Sections.SingleOrDefault(s =>
                    (s.Crn == testSection.Section.Crn) &&
                    (s.Type == testSection.Section.Type) &&
                    (s.StartDate == testMeeting.StartDate) &&
                    (s.EndDate == testMeeting.EndDate) &&
                    (s.Capacity == testSection.Section.Capacity) &&
                    (s.Enrolled == testSection.Section.Enrolled) &&
                    (s.RemainingSpace == testSection.Section.RemainingSpace) &&
                    (s.WaitListCapacity == testSection.Section.WaitListCapacity) &&
                    (s.WaitListCount == testSection.Section.WaitListCount) &&
                    (s.WaitListSpace == testSection.Section.WaitListSpace)));
                // Instructor
                Assert.NotNull(dbContext.Instructors.SingleOrDefault(i =>
                    (i.Email == testMeeting.Instructors.First().email) &&
                    (i.Name == testMeeting.Instructors.First().name)));
                // Meeting
                Assert.NotNull(dbContext.Meetings.SingleOrDefault(m => 
                    (m.Section.Crn == testSection.Section.Crn) &&
                    (m.Instructors.First().Email == testMeeting.Instructors.First().email) &&
                    (m.Type == testMeeting.Type) &&
                    (m.StartDate == testMeeting.StartDate) &&
                    (m.EndDate == testMeeting.EndDate) &&
                    ((DaysOfWeek)m.DaysOfWeek == testMeeting.DaysOfWeek) &&
                    (m.StartTime == testMeeting.StartTime) &&
                    (m.Duration == testMeeting.EndTime.Subtract(testMeeting.StartTime)) &&
                    (m.Room.Number == testSection.Room.Number) &&
                    (m.Room.Building.ShortCode == testSection.Room.Building.Code)));
            }
        }

        [Fact]
        public async Task MultipleSectionClassSyncTest()
        {
            using (var dbContext = GetDbContext())
            {
                var lectureSection = GenerateSection(type: "Lecture", linkSelf: "A0",
                    linkOther: "A1");
                var recitationSection = GenerateSection(course: lectureSection.Course,
                    campus: lectureSection.Campus, type: "Recitation", linkSelf: "A1",
                    linkOther: "A2");
                var labSection = GenerateSection(course: lectureSection.Course,
                    campus: lectureSection.Campus, type: "Laboratory", linkSelf: "A2",
                    linkOther: "A0");
                var scraper = GetScraper(new List<ScrapedSection>() {
                    lectureSection.Section, recitationSection.Section, labSection.Section });
                await Synchronizer.SynchronizeAsync(scraper, dbContext);

                Assert.NotNull(dbContext.Courses.SingleOrDefault(c =>
                    (c.Number == lectureSection.Course.Number) &&
                    (c.Title == lectureSection.Course.Name)));

                Assert.NotNull(dbContext.Classes.SingleOrDefault(c =>
                    (c.Course.Number == lectureSection.Course.Number) &&
                    (c.Course.Title == lectureSection.Course.Name) &&
                    (c.Sections.Any(s => (s.Crn == lectureSection.Section.Crn))) &&
                    (c.Sections.Any(s => (s.Crn == recitationSection.Section.Crn))) &&
                    (c.Sections.Any(s => (s.Crn == labSection.Section.Crn)))));
            }
        }

        [Fact]
        public async Task InstructorAddRemoveSyncTest()
        {
            using (var dbContext = GetDbContext())
            {
                var instructors = new List<TestInstructor>()
                {
                    GenerateInstructor(),
                    GenerateInstructor(),
                    GenerateInstructor(),
                };
                var section = GenerateSection(instructors: instructors);
                var scraper = GetScraper(section.Section);
                await Synchronizer.SynchronizeAsync(scraper, dbContext);

                // Confirm that all instructors were synchronized
                var dbSection = dbContext.Sections
                    .Include(s => s.Meetings)
                    .ThenInclude(m => m.Instructors)
                    .SingleOrDefault(s => s.Crn == section.Section.Crn);
                Assert.NotNull(dbSection);
                var dbMeeting = dbSection.Meetings.FirstOrDefault();
                Assert.NotNull(dbMeeting);
                foreach (var instructor in instructors)
                {
                    Assert.Single(dbMeeting.Instructors, i => (i.Email == instructor.Email));
                }

                // Remove an instructor and sync again
                var removedInstructor = instructors.LastOrDefault();
                instructors.RemoveAt(instructors.Count - 1);
                section.Section.Meetings[0] = section.Section.Meetings[0] with { 
                    Instructors = section.Section.Meetings[0].Instructors
                        .Where(i => (i.email != removedInstructor.Email)).ToArray() };
                scraper = GetScraper(section.Section);
                await Synchronizer.SynchronizeAsync(scraper, dbContext);

                // Confirm that the instructor was removed
                dbSection = dbContext.Sections
                    .Include(s => s.Meetings)
                    .ThenInclude(m => m.Instructors)
                    .SingleOrDefault(s => s.Crn == section.Section.Crn);
                Assert.NotNull(dbSection);
                dbMeeting = dbSection.Meetings.FirstOrDefault();
                Assert.NotNull(dbMeeting);
                Assert.Equal(instructors.Count, dbMeeting.Instructors.Count);
                foreach (var instructor in instructors)
                {
                    Assert.Single(dbMeeting.Instructors, i => (i.Email == instructor.Email));
                }
                Assert.DoesNotContain(dbMeeting.Instructors,
                    i => (i.Email == removedInstructor.Email));

                // Add a new instructor and sync again
                var newInstructor = GenerateInstructor();
                instructors.Add(newInstructor);
                section.Section.Meetings[0] = section.Section.Meetings[0] with { 
                    Instructors = section.Section.Meetings[0].Instructors
                        .Concat(new (string name, string email)[] {
                            (newInstructor.Name, newInstructor.Email) }).ToArray() };
                scraper = GetScraper(section.Section);
                await Synchronizer.SynchronizeAsync(scraper, dbContext);

                // Confirm all the expected instructors were synchronized
                dbSection = dbContext.Sections
                    .Include(s => s.Meetings)
                    .ThenInclude(m => m.Instructors)
                    .SingleOrDefault(s => s.Crn == section.Section.Crn);
                Assert.NotNull(dbSection);
                dbMeeting = dbSection.Meetings.FirstOrDefault();
                Assert.NotNull(dbMeeting);
                Assert.Equal(instructors.Count, dbMeeting.Instructors.Count);
                foreach (var instructor in instructors)
                {
                    Assert.Single(dbMeeting.Instructors, i => (i.Email == instructor.Email));
                }
            }
        }

        [Fact]
        public async Task SectionAddRemoveSyncTest()
        {
            using (var dbContext = GetDbContext())
            {
                // Sync a class with two sections
                var lectureSection = GenerateSection(type: "Lecture", linkSelf: "A0",
                    linkOther: "A1");
                var recitationSection = GenerateSection(course: lectureSection.Course,
                    campus: lectureSection.Campus, type: "Recitation", linkSelf: "A1",
                    linkOther: "A0");
                var scraper = GetScraper(new List<ScrapedSection>() {
                    lectureSection.Section, recitationSection.Section });
                await Synchronizer.SynchronizeAsync(scraper, dbContext);

                // Confirm class w/ two sections was properly synced
                Assert.NotNull(dbContext.Courses.SingleOrDefault(c =>
                    (c.Number == lectureSection.Course.Number) &&
                    (c.Title == lectureSection.Course.Name)));
                Assert.NotNull(dbContext.Classes.SingleOrDefault(c =>
                    (c.Course.Number == lectureSection.Course.Number) &&
                    (c.Course.Title == lectureSection.Course.Name) &&
                    (c.Sections.Count == 2) &&
                    (c.Sections.Any(s => (s.Crn == lectureSection.Section.Crn))) &&
                    (c.Sections.Any(s => (s.Crn == recitationSection.Section.Crn)))));

                // Add a new section to the class and sync again
                var labSection = GenerateSection(course: lectureSection.Course,
                    campus: lectureSection.Campus, type: "Laboratory", linkSelf: "A2",
                    linkOther: "A0");
                recitationSection.Section = recitationSection.Section with { LinkOther = "A2" };
                scraper = GetScraper(new List<ScrapedSection>() {
                    lectureSection.Section, recitationSection.Section, labSection.Section });
                await Synchronizer.SynchronizeAsync(scraper, dbContext);

                // Confirm all three sections are synced
                Assert.NotNull(dbContext.Classes.SingleOrDefault(c =>
                    (c.Course.Number == lectureSection.Course.Number) &&
                    (c.Course.Title == lectureSection.Course.Name) &&
                    (c.Sections.Count == 3) &&
                    (c.Sections.Any(s => (s.Crn == lectureSection.Section.Crn))) &&
                    (c.Sections.Any(s => (s.Crn == recitationSection.Section.Crn))) &&
                    (c.Sections.Any(s => (s.Crn == labSection.Section.Crn)))));

                // Remove a section and sync again
                labSection.Section = labSection.Section with { LinkSelf = "A1", LinkOther = "A0" };
                scraper = GetScraper(new List<ScrapedSection>() {
                    lectureSection.Section, labSection.Section });
                await Synchronizer.SynchronizeAsync(scraper, dbContext);

                // Confirm only two sections are now part of the class, and that the third has
                // been removed.
                Assert.NotNull(dbContext.Classes.SingleOrDefault(c =>
                    (c.Course.Number == lectureSection.Course.Number) &&
                    (c.Course.Title == lectureSection.Course.Name) &&
                    (c.Sections.Count == 2) &&
                    (c.Sections.Any(s => (s.Crn == lectureSection.Section.Crn))) &&
                    (c.Sections.Any(s => (s.Crn == labSection.Section.Crn)))));
                Assert.Null(dbContext.Sections
                    .SingleOrDefault(s => (s.Crn == recitationSection.Section.Crn)));

                // Remove all sections and sync again
                scraper = GetScraper(new List<ScrapedSection>() { });
                await Synchronizer.SynchronizeAsync(scraper, dbContext);

                // Confirm that there are no more sections, and the class has been removed
                Assert.Equal(0, dbContext.Sections.Count());
                Assert.Null(dbContext.Classes.SingleOrDefault(c => 
                    (c.Course.Number == lectureSection.Course.Number) && 
                    (c.Course.Title == lectureSection.Course.Name)));
            }
        }

        private class TestSubject
        {
            public string Code;
            public string Name;
        }

        private class TestCourse
        {
            public TestSubject Subject;
            public string Number;
            public string Name;
            public string Description;
            public double CreditHours;
        }

        private class TestInstructor
        {
            public string Name;
            public string Email;
        }

        private class TestSection
        {
            public ICollection<TestInstructor> Instructors;
            public TestCourse Course;
            public TestCampus Campus;
            public TestRoom Room;
            public ScrapedSection Section;
        }

        private class TestCampus
        {
            public string Name;
            public string Code;
        }

        private class TestBuilding
        {
            public string Name;
            public string Code;
        }

        private class TestRoom
        {
            public string Number;
            public TestBuilding Building;
        }

        private char lastGeneratedSubjectCodeSuffix = 'A';
        private int lastGeneratedCourseNumber = 0;
        private int lastGeneratedCrnNumber = 0;
        private char lastGeneratedInstructorSuffix = 'A';
        private char lastGeneratedCampusSuffix = 'A';
        private char lastGeneratedBuildingSuffix = 'A';
        private int lastGeneratedRoomNumber = 0;
        private readonly (string name, string code) DefaultTerm = ("Fall 2021", "202210");
        private readonly (string name, string code) DefaultSubject = ("Test Subject", "TEST");
        private readonly TimeZoneInfo timeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        private ApplicationDbContext GetDbContext()
        {
            var loggerFactory = new LoggerFactory(new[] { 
                new Microsoft.Extensions.Logging.Debug.DebugLoggerProvider()
            });
            return new ApplicationDbContext(Path.GetTempFileName(), loggerFactory);
        }

        private IScraper GetScraper(ScrapedSection section)
        {
            return GetScraper(new List<ScrapedSection>() { section });
        }

        private IScraper GetScraper(ICollection<ScrapedSection> sections)
        {
            return new MockScraper(
                new List<ScrapedTerm>()
                {
                    new ScrapedTerm()
                    {
                        Id = DefaultTerm.code,
                        Name = DefaultTerm.name,
                    }
                },
                new List<ScrapedSubject>()
                {
                    new ScrapedSubject()
                    {
                        Code = DefaultSubject.code,
                        Name = DefaultSubject.name,
                    }
                },
                sections);
        }

        private TestSubject GenerateSubject()
        {
            var subjectCodeSuffix = lastGeneratedSubjectCodeSuffix++;
            return new TestSubject()
            {
                Code = $"T{subjectCodeSuffix}",
                Name = $"Test Subject {subjectCodeSuffix}",
            };
        }

        private TestCourse GenerateCourse(TestSubject subject = null, double creditHours = 3.14)
        {
            if (subject == null)
            {
                subject = GenerateSubject();
            }
            var courseNum = lastGeneratedCourseNumber++;

            return new TestCourse()
            {
                Subject = subject,
                Number = $"{courseNum}",
                Name = $"Test Course {courseNum}",
                Description = $"This is test course number {courseNum}",
                CreditHours = creditHours,
            };
        }

        private TestInstructor GenerateInstructor()
        {
            var instructorSuffix = lastGeneratedInstructorSuffix++;
            return new TestInstructor()
            {
                Name = $"Instructor {instructorSuffix}",
                Email = $"instructor{instructorSuffix}@university.edu",
            };
        }

        private TestCampus GenerateCampus()
        {
            var campusSuffix = lastGeneratedCampusSuffix++;
            return new TestCampus()
            {
                Name = $"Campus {campusSuffix}",
                Code = $"C{campusSuffix}",
            };
        }

        private TestBuilding GenerateBuilding()
        {
            var buildingSuffix = lastGeneratedBuildingSuffix++;
            return new TestBuilding()
            {
                Name = $"Test Building {buildingSuffix}",
                Code = $"B{buildingSuffix}",
            };
        }

        private TestRoom GenerateRoom(TestBuilding building = null)
        {
            var roomNumber = lastGeneratedRoomNumber++;
            if (building == null)
            {
                building = GenerateBuilding();
            }
            return new TestRoom()
            {
                Number = $"{roomNumber}",
                Building = building,
            };
        }

        private TestSection GenerateSection(ICollection<TestInstructor> instructors = null,
            TestCourse course = null, TestCampus campus = null, string type = "Lecture",
            TestRoom room = null, string linkSelf = "", string linkOther = "")
        {
            var crn = lastGeneratedCrnNumber++;
            if (instructors == null)
            {
                instructors = new List<TestInstructor>()
                {
                    GenerateInstructor(),
                };
            }
            if (course == null)
            {
                course = GenerateCourse();
            }
            if (campus == null)
            {
                campus = GenerateCampus();
            }
            if (room == null)
            {
                room = GenerateRoom();
            }

            return new TestSection()
            {
                Instructors = instructors,
                Course = course,
                Campus = campus,
                Room = room,
                Section = new ScrapedSection()
                {
                    Crn = $"{crn}",
                    SectionCode = "000",
                    Meetings = new ScrapedMeeting[]
                    {
                        new ScrapedMeeting()
                        {
                            Type = type,
                            Instructors = 
                                instructors.Select(i => (name: i.Name, email: i.Email)).ToArray(),
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
                            BuildingCode = room.Building.Code,
                            BuildingName = room.Building.Name,
                            RoomNumber = room.Number,
                        },
                    },
                    SubjectCode = course.Subject.Code,
                    CourseNumber = course.Number,
                    Type = type,
                    CourseTitle = course.Name,
                    Description = course.Description,
                    CreditHours = course.CreditHours,
                    LinkSelf = linkSelf,
                    LinkOther = linkOther,
                    CampusCode = campus.Code,
                    CampusName = campus.Name,
                    Capacity = 32,
                    Enrolled = 16,
                    RemainingSpace = 16,
                    WaitListCapacity = 8,
                    WaitListCount = 4,
                    WaitListSpace = 4,
                },
            };
        }
    }
}