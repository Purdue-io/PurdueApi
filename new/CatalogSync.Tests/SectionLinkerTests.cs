using PurdueIo.Scraper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PurdueIo.CatalogSync.Tests
{
    public class SectionLinkerTests
    {
        [Fact]
        public void BasicSectionLinking()
        {
            var ungroupedSections = new List<Section>()
            {
                // WEE 10100 (first class)
                // Lecture
                new Section()
                {
                    Crn = "12345",
                    SubjectCode = "WEE",
                    CourseNumber = "10100",
                    Type = "Lecture",
                    LinkSelf = "A1",
                    LinkOther = "A3",
                },
                // Recitation
                new Section()
                {
                    Crn = "12346",
                    SubjectCode = "WEE",
                    CourseNumber = "10100",
                    Type = "Recitation",
                    LinkSelf = "A3",
                    LinkOther = "A2",
                },
                // Lab
                new Section()
                {
                    Crn = "12347",
                    SubjectCode = "WEE",
                    CourseNumber = "10100",
                    Type = "Laboratory",
                    LinkSelf = "A2",
                    LinkOther = "A1",
                },

                // WEE 10100 (second class)
                // Lecture
                new Section()
                {
                    Crn = "12350",
                    SubjectCode = "WEE",
                    CourseNumber = "10100",
                    Type = "Lecture",
                    LinkSelf = "A4",
                    LinkOther = "A6",
                },
                // Recitation
                new Section()
                {
                    Crn = "12351",
                    SubjectCode = "WEE",
                    CourseNumber = "10100",
                    Type = "Recitation",
                    LinkSelf = "A6",
                    LinkOther = "A5",
                },
                // Lab
                new Section()
                {
                    Crn = "12352",
                    SubjectCode = "WEE",
                    CourseNumber = "10100",
                    Type = "Laboratory",
                    LinkSelf = "A5",
                    LinkOther = "A4",
                },

                // WEE 10200 (class w/ no links)
                new Section()
                {
                    Crn = "12353",
                    SubjectCode = "WEE",
                    CourseNumber = "10200",
                    Type = "Lecture",
                    LinkSelf = "",
                    LinkOther = "",
                },
                // Another WEE 10200 (class w/ no links)
                new Section()
                {
                    Crn = "12354",
                    SubjectCode = "WEE",
                    CourseNumber = "10200",
                    Type = "Lecture",
                    LinkSelf = "",
                    LinkOther = "",
                },
                // Another WEE 10200 (class w/ no links)
                new Section()
                {
                    Crn = "12355",
                    SubjectCode = "WEE",
                    CourseNumber = "10200",
                    Type = "Lecture",
                    LinkSelf = "",
                    LinkOther = "",
                },

                // WEE 20100 (re-uses link codes from WEE 10100)
                // Lecture
                new Section()
                {
                    Crn = "12360",
                    SubjectCode = "WEE",
                    CourseNumber = "20100",
                    Type = "Lecture",
                    LinkSelf = "A1",
                    LinkOther = "A3",
                },
                // Recitation
                new Section()
                {
                    Crn = "12361",
                    SubjectCode = "WEE",
                    CourseNumber = "20100",
                    Type = "Recitation",
                    LinkSelf = "A3",
                    LinkOther = "A2",
                },
                // Lab
                new Section()
                {
                    Crn = "12362",
                    SubjectCode = "WEE",
                    CourseNumber = "20100",
                    Type = "Laboratory",
                    LinkSelf = "A2",
                    LinkOther = "A1",
                },
            };

            var groupedSections = SectionLinker.GroupLinkedSections(ungroupedSections);

            // We expect 6 groups
            Assert.Equal(6, groupedSections.Count);

            // First WEE 10100 class
            Assert.NotNull(groupedSections
                .SingleOrDefault(g => (
                    (g.Count == 3) &&
                    (g.Any(s => s.Crn == "12345")) &&
                    (g.Any(s => s.Crn == "12346")) &&
                    (g.Any(s => s.Crn == "12347")))));

            // Second WEE 10100 class
            Assert.NotNull(groupedSections
                .SingleOrDefault(g => (
                    (g.Count == 3) &&
                    (g.Any(s => s.Crn == "12350")) &&
                    (g.Any(s => s.Crn == "12351")) &&
                    (g.Any(s => s.Crn == "12352")))));

            // Independent WEE 10200 classes
            Assert.NotNull(groupedSections
                .SingleOrDefault(g => (
                    (g.Count == 1) &&
                    (g.Any(s => s.Crn == "12353")))));
            Assert.NotNull(groupedSections
                .SingleOrDefault(g => (
                    (g.Count == 1) &&
                    (g.Any(s => s.Crn == "12354")))));
            Assert.NotNull(groupedSections
                .SingleOrDefault(g => (
                    (g.Count == 1) &&
                    (g.Any(s => s.Crn == "12355")))));

            // WEE 20100 class
            Assert.NotNull(groupedSections
                .SingleOrDefault(g => (
                    (g.Count == 3) &&
                    (g.Any(s => s.Crn == "12360")) &&
                    (g.Any(s => s.Crn == "12361")) &&
                    (g.Any(s => s.Crn == "12362")))));
        }
    }
}
