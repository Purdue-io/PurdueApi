using PurdueIo.Scraper;
using PurdueIo.Scraper.Models;
using PurdueIo.Tests.Mocks;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PurdueIo.Tests
{
    public class ParsingTests
    {
        [Fact]
        public async Task SectionParsing()
        {
            var connection = new MockMyPurdueConnection();
            var scraper = new MyPurdueScraper(connection);
            var sections = await scraper.GetSectionsAsync("202210", "COM");
            Assert.NotEmpty(sections);
            Assert.Equal(271, sections.Count);

            // Spot check a section with multiple meetings
            Section spotCheck = sections.SingleOrDefault(s => s.Crn == "21497");
            Assert.NotNull(spotCheck);
            Assert.Equal("840", spotCheck.SectionCode);
            Assert.Equal("COM", spotCheck.SubjectCode);
            Assert.Equal("11400", spotCheck.CourseNumber);
            Assert.Equal("Lecture", spotCheck.Type);
            Assert.Equal("Fundamentals Of Speech Communication", spotCheck.CourseTitle);
            Assert.Equal(3, spotCheck.CreditHours);
            Assert.Equal("", spotCheck.LinkSelf);
            Assert.Equal("", spotCheck.LinkOther);
            Assert.Equal("PWL", spotCheck.CampusCode);
            Assert.Equal("West Lafayette Campus", spotCheck.CampusName);
            Assert.Equal(24, spotCheck.Capacity);
            Assert.Equal(5, spotCheck.Enrolled);
            Assert.Equal(19, spotCheck.RemainingSpace);
            Assert.Equal(0, spotCheck.WaitListCapacity);
            Assert.Equal(0, spotCheck.WaitListCount);
            Assert.Equal(0, spotCheck.WaitListSpace);
            Assert.Equal(2, spotCheck.Meetings.Length);
            // First meeting
            Meeting spotCheckMeeting = spotCheck.Meetings[0];
            Assert.Equal("Lecture", spotCheckMeeting.Type);
            Assert.NotEmpty(spotCheckMeeting.Instructors);
            Assert.Equal("Fake E. Instructor", spotCheckMeeting.Instructors[0].name);
            Assert.Equal("donotreply@purdue.edu", spotCheckMeeting.Instructors[0].email);
            Assert.Equal(2021, spotCheckMeeting.StartDate.Year);
            Assert.Equal(8, spotCheckMeeting.StartDate.Month);
            Assert.Equal(23, spotCheckMeeting.StartDate.Day);
            Assert.Equal(2021, spotCheckMeeting.EndDate.Year);
            Assert.Equal(12, spotCheckMeeting.EndDate.Month);
            Assert.Equal(11, spotCheckMeeting.EndDate.Day);
            Assert.Equal(DaysOfWeek.Monday, spotCheckMeeting.DaysOfWeek);
            Assert.Equal(11, spotCheckMeeting.StartTime.Hour);
            Assert.Equal(30, spotCheckMeeting.StartTime.Minute);
            Assert.Equal(12, spotCheckMeeting.EndTime.Hour);
            Assert.Equal(20, spotCheckMeeting.EndTime.Minute);
            Assert.Equal("BRNG", spotCheckMeeting.BuildingCode);
            Assert.Equal("Beering Hall of Lib Arts & Ed", spotCheckMeeting.BuildingName);
            Assert.Equal("B232", spotCheckMeeting.RoomNumber);
            // Second meeting
            spotCheckMeeting = spotCheck.Meetings[1];
            Assert.Equal("Lecture", spotCheckMeeting.Type);
            Assert.NotEmpty(spotCheckMeeting.Instructors);
            Assert.Equal("Fake E. Instructor", spotCheckMeeting.Instructors[0].name);
            Assert.Equal("donotreply@purdue.edu", spotCheckMeeting.Instructors[0].email);
            Assert.Equal(2021, spotCheckMeeting.StartDate.Year);
            Assert.Equal(8, spotCheckMeeting.StartDate.Month);
            Assert.Equal(23, spotCheckMeeting.StartDate.Day);
            Assert.Equal(2021, spotCheckMeeting.EndDate.Year);
            Assert.Equal(12, spotCheckMeeting.EndDate.Month);
            Assert.Equal(11, spotCheckMeeting.EndDate.Day);
            Assert.Equal((DaysOfWeek.Tuesday | DaysOfWeek.Thursday), spotCheckMeeting.DaysOfWeek);
            Assert.Equal(11, spotCheckMeeting.StartTime.Hour);
            Assert.Equal(30, spotCheckMeeting.StartTime.Minute);
            Assert.Equal(12, spotCheckMeeting.EndTime.Hour);
            Assert.Equal(20, spotCheckMeeting.EndTime.Minute);
            Assert.Equal("BRNG", spotCheckMeeting.BuildingCode);
            Assert.Equal("Beering Hall of Lib Arts & Ed", spotCheckMeeting.BuildingName);
            Assert.Equal("1222", spotCheckMeeting.RoomNumber);
        }

        [Fact]
        public async Task SubjectParsing()
        {
            var connection = new MockMyPurdueConnection();
            var scraper = new MyPurdueScraper(connection);
            ICollection<Subject> subjects = await scraper.GetSubjectsAsync("202210");

            // Spot check a few subjects
            Assert.Contains(subjects,
                (s => s.Code == "AAE" && s.Name == "Aero & Astro Engineering"));
            Assert.Contains(subjects,
                (s => s.Code == "CS" && s.Name == "Computer Sciences"));
            Assert.Contains(subjects,
                (s => s.Code == "EAPS" && s.Name == "Earth Atmos Planetary Sci"));
            Assert.Contains(subjects,
                (s => s.Code == "HSOP" && s.Name == "Hlth,Srvcs,Outcomes&Polic"));
            Assert.Contains(subjects,
                (s => s.Code == "PSY" && s.Name == "Psychological Sciences"));
            Assert.Contains(subjects,
                (s => s.Code == "WGSS" && s.Name == "Women Gend&Sexuality Std"));
        }

        [Fact]
        public async Task TermParsing()
        {
            var connection = new MockMyPurdueConnection();
            var scraper = new MyPurdueScraper(connection);
            ICollection<Term> terms = await scraper.GetTermsAsync();

            // Spot check a few terms
            Assert.Contains(terms, (t => t.Id == "202210" && t.Name == "Fall 2021"));
            Assert.Contains(terms, (t => t.Id == "201520" && t.Name == "Spring 2015")); // 🎓
            Assert.Contains(terms, (t => t.Id == "201210" && t.Name == "Fall 2011")); // 🎒
            Assert.Contains(terms, (t => t.Id == "200910" && t.Name == "Fall 2008"));
        }
    }
}
