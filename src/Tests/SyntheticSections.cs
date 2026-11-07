using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PurdueIo.Scraper.Connections.Models;

namespace PurdueIo.Tests
{
    // Builders for small, hand-written MyPurdue scrape data, serialized to the same JSON
    // shape the live service returns. Used to drive FastSync through specific scenarios
    // (removed sections, changed meetings, ...) that the recorded fixture can't express.
    public static class SyntheticSections
    {
        public const string SyncTerm = "202720";
        public const string SyncSubject = "CS";
        public const string CampusCode = "PWL";
        public const string CampusName = "West Lafayette";

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
            string subject = SyncSubject) =>
            new()
            {
                Id = long.Parse(crn),
                TermCode = SyncTerm,
                TermName = "Spring 2027",
                Crn = crn,
                CourseNumber = courseNumber,
                SubjectCode = subject,
                SubjectName = subject,
                CampusName = CampusName,
                SectionType = type,
                CourseTitle = title,
                CreditHours = creditHours,
                CreditHourLow = creditHourLow,
                CreditHourHigh = creditHourHigh,
                Faculty = (faculty ?? [Faculty("Ada Lovelace", "alovelace@purdue.edu")]).ToList(),
                Meetings = (meetings ?? [Meeting()])
                    .Select(t => new MyPurdueMeeting
                    {
                        Crn = crn,
                        TermCode = SyncTerm,
                        MeetingTime = t,
                    })
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
            string endDate = "05/01/2027") =>
            new()
            {
                BeginTime = begin,
                EndTime = end,
                BuildingCode = building,
                BuildingName = building == null ? null : $"{building} Building",
                Room = building == null ? null : room,
                CampusCode = CampusCode,
                CampusName = CampusName,
                StartDate = startDate,
                EndDate = endDate,
                TermCode = SyncTerm,
                IsSunday = days.Contains('U'),
                IsMonday = days.Contains('M'),
                IsTuesday = days.Contains('T'),
                IsWednesday = days.Contains('W'),
                IsThursday = days.Contains('R'),
                IsFriday = days.Contains('F'),
                IsSaturday = days.Contains('S'),
            };

        public static MyPurdueFaculty Faculty(string name, string email) =>
            new() { Name = name, Email = email, TermCode = SyncTerm };

        // A complete searchResults page.
        public static string Page(params MyPurdueSection[] sections) =>
            Page(sections.Length, sections);

        // A searchResults page that claims `totalCount` results (for truncation scenarios).
        public static string Page(int totalCount, params MyPurdueSection[] sections) =>
            JsonSerializer.Serialize(new MyPurdueSectionResponse
            {
                Success = true,
                TotalCount = totalCount,
                Sections = sections.ToList(),
            });

        // A fetchLinkedSections response placing `group` in a single class.
        public static string LinkedResponse(params MyPurdueSection[] group) =>
            JsonSerializer.Serialize(new MyPurdueLinkedSectionsResponse
            {
                SectionGroups = [group.ToList()],
            });

        // Replaces the handler's recorded pages with a single synthetic page.
        public static void Serve(FakeMyPurdueHandler handler, string page)
        {
            var pages = handler.SectionSearchPagesFor(SyncTerm, SyncSubject);
            pages.Clear();
            pages[0] = page;
        }
    }
}
