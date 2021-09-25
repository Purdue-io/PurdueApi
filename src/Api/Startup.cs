using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
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
        }

        // This method gets called by the runtime.
        // Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

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
