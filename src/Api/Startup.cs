using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PurdueIo.Database;
using Microsoft.EntityFrameworkCore;

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
            services.AddDbContext<ApplicationDbContext>(opt => opt
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .UseSqlite($"Data Source=C:\\Users\\Hayden\\Source\\PurdueApi\\src\\CatalogSync\\purdueio.sqlite"));

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

            app.UseHttpsRedirection();

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
