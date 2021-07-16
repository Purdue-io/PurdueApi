using Microsoft.EntityFrameworkCore;
using PurdueIo.Database;
using PurdueIo.Database.Models;
using PurdueIo.Scraper;
using PurdueIo.Scraper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DatabaseTerm = PurdueIo.Database.Models.Term;
using DatabaseSubject = PurdueIo.Database.Models.Subject;
using ScrapedSubject = PurdueIo.Scraper.Models.Subject;
using DatabaseSection = PurdueIo.Database.Models.Section;
using ScrapedSection = PurdueIo.Scraper.Models.Section;

namespace PurdueIo.CatalogSync
{
    public class Synchronizer
    {
        public static Task SynchronizeAsync(IScraper scraper, ApplicationDbContext dbContext)
        {

        }

        public static Task SynchronizeTermAsync(string termCode, IScraper scraper,
            ApplicationDbContext dbContext)
        {

        }

        private IScraper scraper;

        private ApplicationDbContext dbContext;

        private Synchronizer(IScraper scraper, ApplicationDbContext dbContext)
        {
            this.scraper = scraper;
            this.dbContext = dbContext;

            // Disable EF query tracking to improve performance
            // (we'll do it all ourselves)
            this.dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }
        
        private async Task SynchronizeTermSubject(DatabaseTerm dbTerm,
            ScrapedSubject scrapedSubject)
        {
            // Fetch or create the subject in the DB
            var dbSubject = dbContext.Subjects
                .SingleOrDefault(s => EF.Functions.Like(s.Abbreviation, scrapedSubject.Code));
            if (dbSubject == null)
            {
                dbSubject = new DatabaseSubject()
                {
                    Id = Guid.NewGuid(),
                    Abbreviation = scrapedSubject.Code,
                    Name = scrapedSubject.Name,
                };
                dbContext.Subjects.Add(dbSubject);
                dbContext.SaveChanges();
            }

            // Cache courses for this subject
            Dictionary<(string number, string title), Course> dbCourseCache = dbContext.Courses
                .Where(c => c.SubjectId == dbSubject.Id)
                .ToDictionary(c => (c.Number, c.Title));

            // Cache sections
            Dictionary<string, DatabaseSection> dbSectionCache = dbContext.Sections
                .Include(s => s.Class)
                .Include(s => s.Meetings)
                .ThenInclude(m => m.Room)
                .ThenInclude(r => r.Building)
                .Where(s => s.Class.TermId == dbTerm.Id && s.Class.Course.SubjectId == dbSubject.Id)
                .ToDictionary(s => s.Crn);

            // Scrape sections
            ICollection<ScrapedSection> scrapedSections
                = await scraper.GetSectionsAsync(dbTerm.Code, scrapedSubject.Code);
        }

        // Takes a flat list of MyPurdueSection objects and groups them together
        // based on their LinkSelf and LinkOther values.
        private ICollection<ICollection<ScrapedSection>> GroupLinkedSections(
            ICollection<ScrapedSection> scrapedSections)
        {
            var sectionsByLinks = new Dictionary<Tuple<string, string>, LinkSections>();
            var independentSections = new List<MyPurdueSection>();
            foreach (var section in sections)
            {
                if (section.LinkSelf == "" && section.LinkOther == "")
                {
                    independentSections.Add(section);
                    continue;
                }
                var linkSelfTuple = new Tuple<string, string>(section.LinkSelf, section.Number);
                var linkOtherTuple = new Tuple<string, string>(section.LinkOther, section.Number);
                LinkSections linkSelfGroups = null;
                sectionsByLinks.TryGetValue(linkSelfTuple, out linkSelfGroups);
                LinkSections linkOtherGroups = null;
                sectionsByLinks.TryGetValue(linkOtherTuple, out linkOtherGroups);
                if (linkSelfGroups == null && linkOtherGroups == null)
                {
                    var group = new LinkSections(section.LinkSelf, section.LinkOther, section);
                    sectionsByLinks[linkSelfTuple] = group;
                    sectionsByLinks[linkOtherTuple] = group;
                }
                else if (linkSelfGroups != null && linkOtherGroups == null)
                {
                    linkSelfGroups.Add(section);
                    sectionsByLinks[linkOtherTuple] = linkSelfGroups;
                }
                else if (linkSelfGroups == null && linkOtherGroups != null)
                {
                    linkOtherGroups.Add(section);
                    sectionsByLinks[linkSelfTuple] = linkOtherGroups;
                }
                else if (linkSelfGroups != null && linkOtherGroups != null)
                {
                    if (linkSelfGroups != linkOtherGroups)
                    {
                        linkSelfGroups.Absorb(linkOtherGroups);
                        foreach (var l in linkSelfGroups.Links)
                        {
                            sectionsByLinks[new Tuple<string, string>(l, section.Number)] = linkSelfGroups;
                        }
                    }
                    linkSelfGroups.Add(section);
                }
            }
            var sectionGroups = sectionsByLinks.Values.Distinct().Select(s => s.Sections).ToList();
            sectionGroups.AddRange(independentSections.Select(x => new List<MyPurdueSection>() { x }));
            return sectionGroups;
        }
    }
}