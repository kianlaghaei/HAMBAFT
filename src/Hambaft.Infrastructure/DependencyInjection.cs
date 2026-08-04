using Hambaft.Application;
using Hambaft.Domain;
using Hambaft.Infrastructure.Persistence;
using Hambaft.Infrastructure.Security;
using JasperFx;
using JasperFx.Events;
using JasperFx.Events.Projections;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hambaft.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHambaftInfrastructure(this IServiceCollection services,IConfiguration configuration)
    {
        var connection=configuration.GetConnectionString("Hambaft");
        if(string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("HAMBAFT database is not configured. Create local database hambaft_dev and set ConnectionStrings__Hambaft or the ConnectionStrings:Hambaft User Secret.");
        services.AddMarten(options=>Configure(options,connection)).UseLightweightSessions();
        services.AddScoped<ISessionStore,MartenSessionStore>();
        services.AddSingleton<IPairingCodeGenerator,PairingCodeGenerator>();
        services.AddScoped<SessionRuntime>();
        services.AddHealthChecks().AddCheck("postgresql",new PostgresHealthCheck(connection),tags:["ready"]);
        return services;
    }

    public static void Configure(StoreOptions options,string connection)
    {
        options.Connection(connection);
        options.DatabaseSchemaName="hambaft";
        options.Events.DatabaseSchemaName="hambaft";
        options.Events.StreamIdentity=StreamIdentity.AsGuid;
        options.Events.AppendMode=EventAppendMode.Rich;
        options.Events.MetadataConfig.CorrelationIdEnabled=true;
        options.Events.MetadataConfig.CausationIdEnabled=true;
        options.AutoCreateSchemaObjects=AutoCreate.CreateOrUpdate;
        options.Schema.For<SessionStateView>().DatabaseSchemaName("hambaft");
        options.Schema.For<PublicWorldView>().DatabaseSchemaName("hambaft");
        options.Schema.For<SessionExperienceView>().DatabaseSchemaName("hambaft");
        options.Projections.Add<SessionStateProjection>(ProjectionLifecycle.Inline);
        options.Projections.Add<PublicWorldProjection>(ProjectionLifecycle.Inline);
        options.Projections.Add<SessionExperienceProjection>(ProjectionLifecycle.Inline);
        options.Events.AddEventTypes([typeof(SessionCreated),typeof(TeamAdded),typeof(WorldEntityCreated),typeof(EntityAssignedToTeam),typeof(InitialMetricSet),typeof(InitialMemoryAdded),typeof(InitialRelationshipSet),typeof(SessionStarted),typeof(SessionPaused),typeof(SessionResumed),typeof(SessionCancelled)]);
    }
}
