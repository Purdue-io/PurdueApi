using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PurdueIo.Database;
using Microsoft.EntityFrameworkCore;
using System;

namespace PurdueIo.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var dbProvider = Configuration.GetValue<string>("DbProvider");
            var dbConnectionString = Configuration.GetValue<string>("DbConnectionString");

            if (string.Compare(dbProvider, "Sqlite", true) == 0)
            {
                services.AddDbContext<ApplicationDbContext>(opt => opt
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                    .UseSqlite(dbConnectionString, s => s
                        .MigrationsAssembly("Database.Migrations.Sqlite")
                        .UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)));
            }
            else if (string.Compare(dbProvider, "Npgsql", true) == 0)
            {
                services.AddDbContext<ApplicationDbContext>(opt => opt
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                    .UseNpgsql(dbConnectionString, s => s
                        .MigrationsAssembly("Database.Migrations.Npgsql")
                        .UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)));
            }
            else
            {
                throw new ArgumentException("Invalid DB provider specified.");
            }

            var edmModel = EdmModelBuilder.GetEdmModel();
            services.AddControllers().AddOData(opt => opt.AddRouteComponents("odata", edmModel));

            // OData JSON is highly repetitive and compresses very well. These are public
            // catalog responses with no secrets, so compressing over HTTPS is safe (BREACH
            // needs a secret reflected alongside attacker-controlled input).
            services.AddResponseCompression(opt =>
            {
                opt.EnableForHttps = true;
                opt.Providers.Add<BrotliCompressionProvider>();
                opt.Providers.Add<GzipCompressionProvider>();
            });
        }

        // This method gets called by the runtime.
        // Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Apply pending migrations once at startup. The DbContext is
            // registered as scoped, so this must not live in its constructor
            // (that would run Migrate() on every request).
            using (var scope = app.ApplicationServices.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                    .Database.Migrate();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Must run before anything that writes a response body.
            app.UseResponseCompression();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseODataQueryRequest();
            app.UseODataBatching();

            app.UseRouting();

            app.UseCors(c => c
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin());

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
