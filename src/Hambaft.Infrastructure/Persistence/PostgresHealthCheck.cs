using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Hambaft.Infrastructure.Persistence;

public sealed class PostgresHealthCheck(string connectionString) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,CancellationToken ct=default)
    {
        try { await using var connection=new NpgsqlConnection(connectionString);await connection.OpenAsync(ct);await using var command=new NpgsqlCommand("select 1",connection);await command.ExecuteScalarAsync(ct);return HealthCheckResult.Healthy("PostgreSQL and hambaft_dev are reachable."); }
        catch(Exception ex){return HealthCheckResult.Unhealthy("HAMBAFT PostgreSQL is unavailable. Create hambaft_dev and configure ConnectionStrings__Hambaft or User Secrets.",ex);}
    }
}
