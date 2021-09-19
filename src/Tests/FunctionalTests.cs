using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PurdueIo.CatalogSync;
using PurdueIo.Database;
using PurdueIo.Scraper;
using PurdueIo.Tests.Mocks;
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
            using (var dbContext = GetDbContext())
            {
                var connection = new MockMyPurdueConnection();
                var scraper = new MyPurdueScraper(connection,
                    loggerFactory.CreateLogger<MyPurdueScraper>());
                await FastSync.SynchronizeAsync(scraper, dbContext,
                    loggerFactory.CreateLogger<FastSync>());
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