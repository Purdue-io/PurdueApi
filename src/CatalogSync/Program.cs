using CommandLine;
using Microsoft.Extensions.Logging;
using PurdueIo.Database;
using PurdueIo.Scraper;
using PurdueIo.Scraper.Connections;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static PurdueIo.CatalogSync.FastSync;

namespace PurdueIo.CatalogSync
{
    class ConsoleProgress : Progress<SyncProgress>
    {
        
    }

    class Program
    {
        public class Options
        {
            [Option(shortName: 'u', longName: "user", HelpText = "MyPurdue User Name")]
            public string MyPurdueUser { get; set; }

            [Option(shortName: 'p', longName: "pass", HelpText = "MyPurdue Password")]
            public string MyPurduePass { get; set; }

            [Option(shortName: 't', longName: "terms",
                HelpText = "Term codes to sync (ex. 202210)")]
            public IEnumerable<string> Terms { get; set; }

            [Option(shortName: 's', longName: "subjects",
                HelpText = "Subject codes to sync (ex. CS)")]
            public IEnumerable<string> Subjects { get; set; }
        }

        static async Task Main(string[] args)
        {
            (await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(async options => await RunASync(options)))
                .WithNotParsed(errors => errors.Output());
        }

        static async Task RunASync(Options options)
        {
            string username = options.MyPurduePass ?? 
                Environment.GetEnvironmentVariable("MY_PURDUE_USERNAME");
            string password = options.MyPurduePass ?? 
                Environment.GetEnvironmentVariable("MY_PURDUE_PASSWORD");

            if ((username == null) || (password == null))
            {
                Console.Error.WriteLine("You must provide a MyPurdue username and password " +
                    "to sync course. Use command line options or environment variables " +
                    "MY_PURDUE_USERNAME and MY_PURDUE_PASSWORD.");
                return;
            }

            var loggerFactory = LoggerFactory.Create(b => 
                b.AddSimpleConsole(c => c.TimestampFormat = "hh:mm:ss.fff "));

            var logger = loggerFactory.CreateLogger<Program>();

            Action<SyncProgress> reportProgress = (value) => {
                var percentString = Math
                    .Round(value.Progress * 100.0, 2, MidpointRounding.ToZero)
                    .ToString("0.00");
                logger.LogInformation($"[{percentString}%] {value.Description}");
            };

            var connection = await MyPurdueConnection.CreateAndConnectAsync(username, password,
                loggerFactory.CreateLogger<MyPurdueConnection>());
            var scraper = new MyPurdueScraper(connection,
                loggerFactory.CreateLogger<MyPurdueScraper>());
            var dbContext = new ApplicationDbContext();
            await FastSync.SynchronizeAsync(scraper, dbContext,
                loggerFactory.CreateLogger<FastSync>(), options.Terms, options.Subjects,
                TermSyncBehavior.SyncAllTerms, reportProgress);
        }
    }
}
