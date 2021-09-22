using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PurdueIo.CatalogSync;
using PurdueIo.Database;
using PurdueIo.Scraper;
using PurdueIo.Tests.Mocks;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PurdueIo.Tests
{
    public class FunctionalTests
    {
        private readonly ILoggerFactory loggerFactory;

        public FunctionalTests()
        {
            this.loggerFactory = new NullLoggerFactory();
        }

        [Fact]
        public async Task FunctionalSyncTest()
        {
            using (var dbContext = GetDbContextFactory()())
            {
                var connection = new MockMyPurdueConnection();
                var scraper = new MyPurdueScraper(connection,
                    loggerFactory.CreateLogger<MyPurdueScraper>());
                await FastSync.SynchronizeAsync(scraper, dbContext,
                    loggerFactory.CreateLogger<FastSync>());
            }
        }

        private Func<ApplicationDbContext> GetDbContextFactory(string path = "")
        {
            if (path == "")
            {
                path = Path.GetTempFileName();
            }
            var loggerFactory = new LoggerFactory(new[] { 
                new Microsoft.Extensions.Logging.Debug.DebugLoggerProvider()
            });
            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={path}", 
                    s => s.MigrationsAssembly("Database.Migrations.Sqlite"))
                .UseLoggerFactory(loggerFactory)
                .Options;
            return () => {
                var retVal = new ApplicationDbContext(dbOptions);
                retVal.Database.Migrate();
                return retVal;
            };
        }
    }
}