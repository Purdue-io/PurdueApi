using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PurdueIo.Scraper.Connections;
using Xunit;

namespace PurdueIo.Tests
{
    public class MyPurdueConnectionTests
    {
        private readonly ILoggerFactory loggerFactory;

        public MyPurdueConnectionTests()
        {
            this.loggerFactory = new NullLoggerFactory();
        }

        [Fact]
        public async Task NewMyPurdueConnectionTest()
        {
            var connection = new MyPurdueConnection(
                loggerFactory.CreateLogger<MyPurdueConnection>());
            var sections = await connection.GetSectionsAsync("202720", "CS");
            Assert.True(sections.Count > 0);
        }
    }
}