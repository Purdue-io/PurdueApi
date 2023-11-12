using CommandLine;
using Microsoft.EntityFrameworkCore;
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
    class Program
    {
        public enum DataProvider
        {
            Sqlite,
            Npgsql,
        }

        public class Options
        {
            [Option(shortName: 'd', longName: "data-provider", Default = DataProvider.Sqlite,
                HelpText = "The database provider to use")]
            public DataProvider DataProvider { get; set; }

            [Option(shortName: 'c', longName: "connection-string",
                Default = "Data Source=purdueio.sqlite",
                HelpText = "The connection string used to connect to the database provider")]
            public string ConnectionString { get; set; }

            [Option(shortName: 'a', longName: "sync-all-terms", Default = false,
                HelpText = "Sync all terms, don't skip old/existing terms")]
            public bool SyncAllTerms { get; set; }

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
            var loggerFactory = LoggerFactory.Create(b => 
                b.AddSimpleConsole(c => c.TimestampFormat = "hh:mm:ss.fff "));

            var logger = loggerFactory.CreateLogger<Program>();

            Action<SyncProgress> reportProgress = (value) => {
                var percentString = Math
                    .Round(value.Progress * 100.0, 2, MidpointRounding.ToZero)
                    .ToString("0.00");
                logger.LogInformation($"[{percentString}%] {value.Description}");
            };

            var behavior = options.SyncAllTerms ? 
                TermSyncBehavior.SyncAllTerms : TermSyncBehavior.SyncNewAndCurrentTerms;
            var connection = new MyPurdueConnection(
                loggerFactory.CreateLogger<MyPurdueConnection>());
            var scraper = new MyPurdueScraper(connection,
                loggerFactory.CreateLogger<MyPurdueScraper>());

            ApplicationDbContext dbContext;
            if (options.DataProvider == DataProvider.Sqlite)
            {
                var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlite(options.ConnectionString,
                        o => o.MigrationsAssembly("Database.Migrations.Sqlite"))
                    .Options;
                dbContext = new ApplicationDbContext(dbOptions);
            }
            else if (options.DataProvider == DataProvider.Npgsql)
            {
                var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseNpgsql(options.ConnectionString,
                        o => o.MigrationsAssembly("Database.Migrations.Npgsql"))
                    .Options;
                dbContext = new ApplicationDbContext(dbOptions);
            }
            else
            {
                throw new ArgumentException("Invalid database provider specified.");
            }
            dbContext.Database.Migrate();

            await FastSync.SynchronizeAsync(scraper, dbContext,
                loggerFactory.CreateLogger<FastSync>(), options.Terms, options.Subjects,
                behavior, reportProgress);
        }
    }
}
