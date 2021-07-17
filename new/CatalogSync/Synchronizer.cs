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
            throw new NotImplementedException();
        }

        public static Task SynchronizeTermAsync(string termCode, IScraper scraper,
            ApplicationDbContext dbContext)
        {
            throw new NotImplementedException();
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
    }
}