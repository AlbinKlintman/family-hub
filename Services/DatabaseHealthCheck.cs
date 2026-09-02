using Microsoft.Extensions.Diagnostics.HealthChecks;
using WebApp.Data;

namespace WebApp.Services;

public class DatabaseHealthCheck(ApplicationDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? HealthCheckResult.Healthy("Database connection OK.")
            : HealthCheckResult.Unhealthy("Cannot connect to the database.");
    }
}
