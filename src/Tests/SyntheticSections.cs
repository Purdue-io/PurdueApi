using System.Linq;
using System.Text.Json;
using PurdueIo.Scraper.Connections.Models;

namespace PurdueIo.Tests
{
    // Builders for small, hand-written MyPurdue section data, for driving FastSync through
    // scenarios (removed sections, changed meetings, ...) the recorded fixture can't express.
    // Serve them with FakeMyPurdueHandler.SetSections.
    public static class SyntheticSections
    {
        // Synthetic sections default to the recorded term and subject, which a default
        // FakeMyPurdueHandler already lists, so tests only configure what they vary.
        public const string DefaultTerm = TestResources.RecordedTerm;
        public const string DefaultSubject = TestResources.RecordedSubject;
        public const string DefaultCampusCode = "PWL";
        public const string DefaultCampusName = "West Lafayette";

        public static MyPurdueSection Section(string crn,
            string courseNumber = "18000",
            string title = "Problem Solving And Object-Oriented Programming",
            string type = "Lecture",
            double? creditHours = 3,
            double? creditHourLow = null,
            double? creditHourHigh = null,
            MyPurdueMeetingTime[] meetings = null,
            MyPurdueFaculty[] faculty = null,
            string linkIdentifier = null,
            string term = DefaultTerm,
            string subject = DefaultSubject,
            string campusName = DefaultCampusName) =>
            new()
            {
                Id = long.Parse(crn),
                TermCode = term,
                Crn = crn,
                CourseNumber = courseNumber,
                SubjectCode = subject,
                CampusName = campusName,
                SectionType = type,
                CourseTitle = title,
                CreditHours = creditHours,
                CreditHourLow = creditHourLow,
                CreditHourHigh = creditHourHigh,
                Faculty = (faculty ?? [Faculty("Ada Lovelace", "alovelace@purdue.edu")]).ToList(),
                Meetings = (meetings ?? [Meeting()])
                    .Select(t => new MyPurdueMeeting { Crn = crn, MeetingTime = t })
                    .ToList(),
                IsSectionLinked = linkIdentifier != null,
                LinkIdentifier = linkIdentifier,
                SubjectCourseNumber = subject + courseNumber,
            };

        // `days` uses Banner-style letters: U M T W R F S. Pass building: null for TBA.
        public static MyPurdueMeetingTime Meeting(
            string begin = "0930",
            string end = "1020",
            string building = "LWSN",
            string room = "B151",
            string days = "MWF",
            string startDate = "01/11/2027",
            string endDate = "05/01/2027",
            string campusCode = DefaultCampusCode,
            string campusName = DefaultCampusName) =>
            new()
            {
                BeginTime = begin,
                EndTime = end,
                BuildingCode = building,
                BuildingName = building == null ? null : $"{building} Building",
                Room = building == null ? null : room,
                CampusCode = campusCode,
                CampusName = campusName,
                StartDate = startDate,
                EndDate = endDate,
                IsSunday = days.Contains('U'),
                IsMonday = days.Contains('M'),
                IsTuesday = days.Contains('T'),
                IsWednesday = days.Contains('W'),
                IsThursday = days.Contains('R'),
                IsFriday = days.Contains('F'),
                IsSaturday = days.Contains('S'),
            };

        public static MyPurdueFaculty Faculty(string name, string email) =>
            new() { Name = name, Email = email };

        // One raw searchResults page claiming `totalCount` results. SetSections builds
        // these for you; use this directly only to simulate inconsistent responses.
        public static string Page(int totalCount, params MyPurdueSection[] sections) =>
            JsonSerializer.Serialize(new MyPurdueSectionResponse
            {
                Success = true,
                TotalCount = totalCount,
                Sections = sections.ToList(),
            });
    }
}
