using Hambaft.Infrastructure;
using Marten;
using Npgsql;

namespace Hambaft.IntegrationTests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__HambaftTest")))Skip="Requires local PostgreSQL database hambaft_test and ConnectionStrings__HambaftTest.";
    }
}

[CollectionDefinition("postgres",DisableParallelization=true)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;

public sealed class PostgresFixture : IAsyncLifetime
{
    public string ConnectionString { get; }=Environment.GetEnvironmentVariable("ConnectionStrings__HambaftTest")??string.Empty;
    public IDocumentStore? Store { get; private set; }
    public async Task InitializeAsync()
    {
        if(string.IsNullOrWhiteSpace(ConnectionString))return;
        DatabaseSafety.ValidateTestConnection(ConnectionString);
        await using var connection=new NpgsqlConnection(ConnectionString);await connection.OpenAsync();
        await using var command=new NpgsqlCommand("drop schema if exists hambaft cascade; create schema hambaft",connection);await command.ExecuteNonQueryAsync();
        Store=DocumentStore.For(options=>DependencyInjection.Configure(options,ConnectionString));
        await Store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
    }
    public Task DisposeAsync(){Store?.Dispose();return Task.CompletedTask;}
}

public static class DatabaseSafety
{
    public static void ValidateTestConnection(string connectionString)
    {
        var builder=new NpgsqlConnectionStringBuilder(connectionString);
        if(!string.Equals(builder.Database,"hambaft_test",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Integration tests may target only database hambaft_test.");
        if(connectionString.Contains("sharedworld",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Integration tests must never target SharedWorld.");
    }
}
