using PurdueIo.Scraper;
using PurdueIo.Scraper.Connections;
using System;
using System.Threading.Tasks;

namespace PurdueIo.CatalogSync
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var username = Environment.GetEnvironmentVariable("MY_PURDUE_USERNAME");
            var password = Environment.GetEnvironmentVariable("MY_PURDUE_PASSWORD");
            if (username == null || password == null)
            {
                Console.WriteLine("You must set MY_PURDUE_USERNAME and MY_PURDUE_PASSWORD " +
                    "environment variables.");
                return;
            }
            var connection = await MyPurdueConnection.CreateAndConnectAsync(username, password);
            var scraper = new MyPurdueScraper(connection);
            var terms = await scraper.GetTermsAsync();
            foreach (var term in terms)
            {
                Console.WriteLine(term);
            }

            var subjects = await scraper.GetSubjectsAsync("202210");
            foreach (var subject in subjects)
            {
                Console.WriteLine(subject);
            }

            var sectionList = await scraper.GetSectionsAsync("202210", "CS");
            foreach (var section in sectionList)
            {
                Console.WriteLine(section);
            }
        }
    }
}
