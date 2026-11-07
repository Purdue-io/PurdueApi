using System.Collections.Generic;
using System.Linq;
using PurdueIo.Scraper.Connections.Models;
using Xunit;

namespace PurdueIo.Tests
{
    // Regression coverage for the JSON models the scraper binds to, run against the
    // cached class search responses in Resources\ instead of the live service. If the
    // wire format drifts away from the [JsonPropertyName] mappings, these fail loudly.
    public class CachedSectionResponseTests
    {
        [Theory]
        [InlineData(TestResources.SectionPage0, 500)]
        [InlineData(TestResources.SectionPage500, 89)]
        public void SectionPageParses(string fileName, int expectedRows)
        {
            var response = TestResources.LoadSectionPage(fileName);

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(589, response.TotalCount);
            Assert.Equal(expectedRows, response.Sections.Count);
        }

        [Fact]
        public void PagesCombineIntoAConsistentSet()
        {
            var sections = LoadAllSections();

            Assert.Equal(589, sections.Count);
            var crns = sections.Select(s => s.Crn).ToList();
            Assert.Equal(crns.Count, crns.Distinct().Count());
            Assert.Contains(sections, s => s.IsSectionLinked);
            Assert.All(sections, s =>
            {
                Assert.Equal(TestResources.RecordedTerm, s.TermCode);
                Assert.Equal(TestResources.RecordedSubject, s.SubjectCode);
                Assert.False(string.IsNullOrWhiteSpace(s.Crn));
                Assert.False(string.IsNullOrWhiteSpace(s.CourseNumber));
                Assert.False(string.IsNullOrWhiteSpace(s.CourseTitle));
                Assert.NotEmpty(s.Meetings);
            });
        }

        [Fact]
        public void FirstSectionBindsEveryModelField()
        {
            var first = TestResources.LoadSectionPage(TestResources.SectionPage0)
                .Sections.First();

            Assert.Equal(1281081L, first.Id);
            Assert.Equal(TestResources.RecordedTerm, first.TermCode);
            Assert.Equal("Spring 2027", first.TermName);
            Assert.Equal("10787", first.Crn);
            Assert.Equal("15900", first.CourseNumber);
            Assert.Equal(TestResources.RecordedSubject, first.SubjectCode);
            Assert.Equal("CS-Computer Sciences", first.SubjectName);
            Assert.Equal("Indianapolis and W Lafayette", first.CampusName);
            Assert.Equal("Laboratory", first.SectionType);
            Assert.Equal("C Programming", first.CourseTitle);
            Assert.Equal(0.0, first.CreditHours);
            Assert.Equal(0.0, first.CreditHourLow);
            Assert.Equal(3.0, first.CreditHourHigh);
            Assert.Equal("OR", first.CreditHourIndicator);
            Assert.Equal(30, first.MaximumEnrollment);
            Assert.Equal(1, first.Enrollment);
            Assert.Equal(29, first.SeatsAvailable);
            Assert.True(first.IsSectionLinked);
            Assert.Equal("AF", first.LinkIdentifier);
            Assert.Equal("CS15900", first.SubjectCourseNumber);

            var meeting = Assert.Single(first.Meetings);
            Assert.Equal("01", meeting.Category);
            Assert.Equal("10787", meeting.Crn);
            Assert.Equal(TestResources.RecordedTerm, meeting.TermCode);
            Assert.Empty(meeting.Faculty);

            var time = meeting.MeetingTime;
            Assert.Equal("0930", time.BeginTime);
            Assert.Equal("1120", time.EndTime);
            Assert.Equal("ET", time.BuildingCode);
            Assert.Equal("Engineering/Technology", time.BuildingName);
            Assert.Equal("PIN", time.CampusCode);
            Assert.Equal("006", time.Room);
            Assert.Equal("01/11/2027", time.StartDate);
            Assert.Equal("05/01/2027", time.EndDate);
            Assert.Equal(1.83, time.HoursPerWeek);
            Assert.Equal("LAB", time.ScheduleType);
            Assert.Equal("CLAS", time.MeetingType);
            Assert.Equal("Class", time.MeetingTypeDescription);
            Assert.True(time.IsWednesday);
            Assert.False(time.IsTuesday);
            Assert.False(time.IsThursday);
        }

        [Fact]
        public void HtmlEntitiesInMappedStringsAreDecoded()
        {
            // CRN 10833's raw title contains "Compliance, &amp; Gov"
            var withEntity = Assert.Single(LoadAllSections(), s => s.Crn == "10833");
            Assert.Equal("Cyber Risk, Compliance, & Gov", withEntity.CourseTitle);
        }

        private static List<MyPurdueSection> LoadAllSections() =>
        [
            .. TestResources.LoadSectionPage(TestResources.SectionPage0).Sections,
            .. TestResources.LoadSectionPage(TestResources.SectionPage500).Sections,
        ];
    }
}
