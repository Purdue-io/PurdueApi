using CatalogApi;
using CatalogApi.Models;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using Microsoft.AspNet.Identity.EntityFramework;
using PurdueIoDb;
using PurdueIoDb.Catalog;

namespace CatalogSync
{
	// To learn more about Microsoft Azure WebJobs, please see http://go.microsoft.com/fwlink/?LinkID=401557
	class Program
	{
		private static readonly int MAX_RETRIES = 10;
		private static readonly int RETRY_DELAY_MS = 5000;
		private CatalogApi.CatalogApi Api;
		static int Main()
		{
			var appSettings = ConfigurationManager.AppSettings;
			var p = new Program(appSettings["MyPurdueUser"], appSettings["MyPurduePass"]); // Credentials go here.
			Database.SetInitializer<ApplicationDbContext>(new MigrateDatabaseToLatestVersion<ApplicationDbContext, PurdueIoDb.Migrations.Configuration>());
            p.Start();
            //p.SyncSubject(new MyPurdueTerm() { Id = "201410", Name = "Fall 2013" }, new MyPurdueSubject() { SubjectCode = "EAPS", SubjectName = "Earth Atmos Planetary Sci" }).GetAwaiter().GetResult();
			return 0;
		}

		Program(string user, string pass) {
			Api = new CatalogApi.CatalogApi(user, pass);
			var login = Api.Authenticate().Result;
			if (!login) {
				throw new UnauthorizedAccessException("Could not authenticate to myPurdue with the supplied credentials.");
			}
		}

        public void Start()
        {
            try {
                Task.Run(Synchronize).Wait();
            } catch (Exception e)
            {
                throw e;
            }
        }

		public async Task Synchronize()
		{
			Console.WriteLine(DateTimeOffset.Now.ToString("G") + " Beginning synchronization...");
			var terms = await Api.FetchTermList();
            Console.WriteLine("Found terms:");
            foreach (var term in terms)
            {
                Console.WriteLine("\t " + term.Id + ": " + term.Name);
            }
            // Take STAR out of the list. We don't sync STAR.
            terms = terms.Where(t => !t.Name.ToUpper().StartsWith("STAR")).ToList();
            List<MyPurdueTerm> termsToSync = new List<MyPurdueTerm>();
            using (var db = new ApplicationDbContext())
            {
                var dbTerms = db.Terms.ToList();
                foreach (var term in terms)
                {
                    var dbTerm = dbTerms.SingleOrDefault(t => t.TermCode == term.Id);
                    if (dbTerm == null 
                        || dbTerm.EndDate > DateTimeOffset.Now
                        || dbTerm.StartDate == DateTimeOffset.MinValue
                        || dbTerm.EndDate == DateTimeOffset.MinValue)
                    {
                        termsToSync.Add(term);
                    }
                }
            }
            Console.WriteLine("Synchronizing these terms:");
            foreach (var term in termsToSync)
            {
                Console.WriteLine("\t " + term.Id + ": " + term.Name);
            }
            foreach (var term in termsToSync)
            {
                using (var termSync = new TermSync(term.Id, term.Name, Api))
                {
                    await termSync.Synchronize();
                }
            }
        }
	}
}
