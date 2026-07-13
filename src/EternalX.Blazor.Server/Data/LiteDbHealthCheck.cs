using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EternalX.Blazor.Server.Data;

public sealed class LiteDbHealthCheck : IHealthCheck
{
    private readonly LiteDbService _db;

    public LiteDbHealthCheck(LiteDbService db) => _db = db;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Touch settings collection (seeded) without leaving a file handle open.
            _ = _db.GetSettings();
            return Task.FromResult(HealthCheckResult.Healthy("LiteDB reachable"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("LiteDB unreachable", ex));
        }
    }
}
