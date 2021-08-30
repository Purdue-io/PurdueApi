using PurdueIo.CatalogSync.Tests.Mocks;
using PurdueIo.Database;
using PurdueIo.Scraper;
using System;
using System.Threading.Tasks;
using Xunit;

namespace PurdueIo.CatalogSync.Tests
{
    public class SynchronizerTests
    {
        [Fact]
        public async Task BasicSynchronizationTest()
        {
            IScraper scraper = new MockScraper();
            var dbContext = new ApplicationDbContext();

            await Synchronizer.SynchronizeAsync(scraper, dbContext);
        }
    }
}