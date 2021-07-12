using System;
using System.Threading.Tasks;
using PurdueIo.Scraper.Tests.Mocks;
using Xunit;

namespace Catalog.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            var connection = new MockMyPurdueConnection();
            System.Diagnostics.Debug.WriteLine("Term List:\n");
            System.Diagnostics.Debug.WriteLine(await connection.GetTermListPageAsync());

            System.Diagnostics.Debug.WriteLine("Subject List:\n");
            System.Diagnostics.Debug.WriteLine(await connection.GetSubjectListPageAsync("202210"));

            System.Diagnostics.Debug.WriteLine("Section List:\n");
            System.Diagnostics.Debug.WriteLine(
                await connection.GetSectionListPageAsync("202210", "CS"));

            System.Diagnostics.Debug.WriteLine("Section Details:\n");
            System.Diagnostics.Debug.WriteLine(
                await connection.GetSectionDetailsPageAsync("202210", "CS"));
        }
    }
}
