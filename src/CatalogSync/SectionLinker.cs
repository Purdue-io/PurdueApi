using System;
using System.Collections.Generic;
using System.Linq;

using ScrapedSection = PurdueIo.Scraper.Models.Section;

namespace PurdueIo.CatalogSync
{
    public static class SectionLinker
    {
        // Sections can be "linked" when more than one is associated with a given class.
        // For example, a single class may have a Lecture section, a Recitation section, and a
        // lab section.
        //
        // MyPurdue indicates which sections are linked to one another by providing a "link self"
        // and "link other" identifier with each section.
        // 
        // For example, PHYS 17200 has a Lecture, Recitation, and Laboratory:
        //     Lecture:    Self = A1, Other = A3
        //     Recitation: Self = A2, Other = A1
        //     Laboratory: Self = A3, Other = A2
        //
        // Identifiers are unique only to the course (other courses may re-use identifiers A1 - A3)
        //
        // This method will trace link identifiers for each course and return groups of 
        // linked sections
        public static ICollection<ICollection<ScrapedSection>> GroupLinkedSections(
            ICollection<ScrapedSection> scrapedSections)
        {
            // Since link identifiers are unique per course number, we can index on the composite
            // of these two values
            var sectionsByLinkIdentifier =
                new Dictionary<(string courseNumber, string linkIdentifier),
                    List<ScrapedSection>>();
            var noLinkSections = new List<ScrapedSection>();
            foreach(var section in scrapedSections)
            {
                if (section.LinkSelf.Length > 0)
                {
                    var key = (section.CourseNumber, section.LinkSelf);
                    if (!sectionsByLinkIdentifier.ContainsKey(key))
                    {
                        sectionsByLinkIdentifier[key] = new List<ScrapedSection>() { section };
                    }
                    else
                    {
                        sectionsByLinkIdentifier[key].Add(section);
                    }
                }
                else
                {
                    noLinkSections.Add(section);
                }
            }

            // Now trace each section's links
            var groupedSections = new List<ICollection<ScrapedSection>>();
            while (sectionsByLinkIdentifier.Count > 0)
            {
                // Grab an arbitrary section/link group and traverse the "linkother" values
                // to create a group of sections
                var sectionGroup = new List<ScrapedSection>();
                var visitedIds = new HashSet<string>();
                var idVisitQueue = new Queue<string>();
                var firstLinkPair = sectionsByLinkIdentifier.FirstOrDefault();
                var courseNumber = firstLinkPair.Key.courseNumber;
                var currentLinkId = firstLinkPair.Key.linkIdentifier;

                while (true)
                {
                    var key = (courseNumber, currentLinkId);
                    if (!visitedIds.Contains(currentLinkId) &&
                        sectionsByLinkIdentifier.ContainsKey(key))
                    {
                        var otherSections = sectionsByLinkIdentifier[key];
                        foreach (var otherSection in otherSections)
                        {
                            if (!visitedIds.Contains(otherSection.LinkOther))
                            {
                                idVisitQueue.Enqueue(otherSection.LinkOther);
                            }
                            sectionGroup.Add(otherSection);
                        }
                    }
                    visitedIds.Add(currentLinkId);

                    if (idVisitQueue.Count <= 0)
                    {
                        break;
                    }
                    else
                    {
                        currentLinkId = idVisitQueue.Dequeue();
                    }
                }

                // Clear entries that have been traversed
                foreach (var linkId in visitedIds)
                {
                    sectionsByLinkIdentifier.Remove((courseNumber, linkId));
                }

                groupedSections.Add(sectionGroup);
            }

            // Add all of the sections without links
            groupedSections.AddRange(noLinkSections.Select(s => new List<ScrapedSection>() { s }));

            return groupedSections;
        }
    }
}