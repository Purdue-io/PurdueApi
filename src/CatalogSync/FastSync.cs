using Microsoft.EntityFrameworkCore;
using PurdueIo.Database;
using PurdueIo.Scraper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DatabaseClass = PurdueIo.Database.Models.Class;
using DatabaseSubject = PurdueIo.Database.Models.Subject;
using DatabaseTerm = PurdueIo.Database.Models.Term;

namespace PurdueIo.CatalogSync
{
    public class FastSync
    {
        public enum TermSyncBehavior
        {
            SyncAllTerms,
            // TODO: SyncCurrentAndNewTerms
        }

        public static async Task SynchronizeAsync(IScraper scraper, ApplicationDbContext dbContext,
            TermSyncBehavior termSyncBehavior = TermSyncBehavior.SyncAllTerms)
        {
            await (new FastSync(scraper, dbContext)).InternalSynchronizeAsync(termSyncBehavior);
        }

        public static async Task SynchronizeAsync(IScraper scraper, ApplicationDbContext dbContext,
            string termCode)
        {
            await (new FastSync(scraper, dbContext)).InternalSynchronizeAsync(TermSyncBehavior.SyncAllTerms,
                termCode);
        }

        public static async Task SynchronizeAsync(IScraper scraper, ApplicationDbContext dbContext,
            string termCode, string subjectCode)
        {
            await (new FastSync(scraper, dbContext)).InternalSynchronizeAsync(TermSyncBehavior.SyncAllTerms,
                termCode, subjectCode);
        }

        private IScraper scraper;

        private ApplicationDbContext dbContext;
        
        private FastSync(IScraper scraper, ApplicationDbContext dbContext)
        {
            this.scraper = scraper;
            this.dbContext = dbContext;

            // Disable change tracking
            this.dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
            this.dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        private async Task InternalSynchronizeAsync(TermSyncBehavior termSyncBehavior,
            string termCode = "", string subjectCode = "")
        {
            // Fetch existing terms from DB
            var terms = dbContext.Terms.ToDictionary(t => t.Code);

            // Scrape new terms from service
            var scrapedTerms = await scraper.GetTermsAsync();

            // Add any new terms to the database
            foreach (var scrapedTerm in scrapedTerms)
            {
                // If a term code was specified, filter to that term only
                if ((termCode.Length > 0) && (scrapedTerm.Id != termCode))
                {
                    continue;
                }
                if (!terms.ContainsKey(scrapedTerm.Id))
                {
                    var dbTerm = new DatabaseTerm()
                    {
                        Id = Guid.NewGuid(),
                        Code = scrapedTerm.Id,
                        Name = scrapedTerm.Name,
                        StartDate = DateTimeOffset.MinValue,
                        EndDate = DateTimeOffset.MaxValue,
                    };
                    dbContext.Add(dbTerm);
                    terms[scrapedTerm.Id] = dbTerm;
                }
            }
            dbContext.SaveChanges();

            // Sync each term
            foreach (var termPair in terms)
            {
                // If a term code was specified, filter to that term only
                if ((termCode.Length > 0) && (termPair.Key != termCode))
                {
                    continue;
                }
                await InternalSynchronizeTermAsync(termPair.Value, subjectCode);
            }
        }

        private async Task InternalSynchronizeTermAsync(DatabaseTerm term, string subjectCode)
        {
            // Fetch existing subjects from DB
            var subjects = dbContext.Subjects.ToDictionary(s => s.Abbreviation);

            // Scrape new subjects from service
            var scrapedSubjects = await scraper.GetSubjectsAsync(term.Code);

            // Add any new subjects to the database
            foreach (var scrapedSubject in scrapedSubjects)
            {
                // If a subject was specified, filter to that subject only
                if ((subjectCode.Length > 0) && (scrapedSubject.Code != subjectCode))
                {
                    continue;
                }
                if (!subjects.ContainsKey(scrapedSubject.Code))
                {
                    var dbSubject = new DatabaseSubject()
                    {
                        Id = Guid.NewGuid(),
                        Name = scrapedSubject.Name,
                        Abbreviation = scrapedSubject.Code,
                    };
                    dbContext.Add(dbSubject);
                    subjects[scrapedSubject.Code] = dbSubject;
                }
            }
            dbContext.SaveChanges();
        }
    }
}