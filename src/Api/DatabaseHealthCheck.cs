using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PurdueIo.Database;

namespace PurdueIo.Api
{
    // Reports unhealthy when the API can't reach its database, which is what the old
    // `GET /odata/Terms` container probe effectively checked.
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext dbContext;

        public DatabaseHealthCheck(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Cannot connect to the database.");
        }
    }
}
