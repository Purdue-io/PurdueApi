using Microsoft.Extensions.Logging;
using PurdueIo.CatalogSync;
using PurdueIo.Database;
using PurdueIo.Scraper;
using PurdueIo.Tests.Mocks;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace PurdueIo.Tests
{
    public class FunctionalTests
    {
        [Fact]
        public async Task FunctionalSyncTest()
        {
            using (var dbContext = GetDbContext())
            {
                var connection = new MockMyPurdueConnection();
                var scraper = new MyPurdueScraper(connection);
                await FastSync.SynchronizeAsync(scraper, dbContext, "202210", "COM");
                //, 
            }
        }

        private ApplicationDbContext GetDbContext()
        {
            var loggerFactory = new LoggerFactory(new[] { 
                new Microsoft.Extensions.Logging.Debug.DebugLoggerProvider()
            });
            return new ApplicationDbContext(Path.GetTempFileName(), loggerFactory);
        }
    }
}