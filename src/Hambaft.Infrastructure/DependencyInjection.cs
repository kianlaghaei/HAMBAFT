using Hambaft.Application;
using Hambaft.Domain;
using Hambaft.Infrastructure.Persistence;
using Hambaft.Infrastructure.Security;
using Hambaft.Infrastructure.StoryPackages;
using Hambaft.Narrative.Ink;
using JasperFx;
using JasperFx.Events;
using JasperFx.Events.Projections;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hambaft.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHambaftInfrastructure(this IServiceCollection services,IConfiguration configuration,string? contentRootPath=null)
    {
        var connection=configuration.GetConnectionString("Hambaft");
        if(string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("HAMBAFT database is not configured. Create local database hambaft_dev and set ConnectionStrings__Hambaft or the ConnectionStrings:Hambaft User Secret.");
        services.AddMarten(options=>Configure(options,connection)).UseLightweightSessions();
        services.AddScoped<ISessionStore,MartenSessionStore>();
        var configuredRoot=configuration[$"{StoryPackageOptions.SectionName}:Root"];
        var basePath=contentRootPath??Directory.GetCurrentDirectory();
        var storyRoot=Path.GetFullPath(configuredRoot is null?Path.Combine(basePath,"..","..","stories"):Path.IsPathRooted(configuredRoot)?configuredRoot:Path.Combine(basePath,configuredRoot));
        services.AddSingleton(new StoryPackageOptions { Root=storyRoot });
        services.AddSingleton(new InkStoryRootOptions(storyRoot));
        services.AddSingleton<IInkStoryLoader,FileInkStoryLoader>();
        services.AddSingleton<IInkStateSerializer,InkStateSerializer>();
        services.AddSingleton<IInkNarrativeRenderer,InkNarrativeRenderer>();
        services.AddSingleton<IInkPackageValidator,InkPackageValidator>();
        services.AddSingleton<IStoryPackageHasher,DeterministicStoryPackageHasher>();
        services.AddSingleton<IStoryPackageValidator,StoryPackageValidator>();
        services.AddSingleton<IStoryPackageLoader,FileSystemStoryPackageLoader>();
        services.AddSingleton<IStoryPackageCatalog,FileSystemStoryPackageCatalog>();
        services.AddSingleton<IConditionEngine,ConditionEngine>();
        services.AddSingleton<IStoryletSelector,DeterministicStoryletSelector>();
        services.AddSingleton<IEffectEngine,EffectEngine>();
        services.AddSingleton<IInteractionTermsValidator,InteractionTermsValidator>();
        services.AddSingleton<IBehaviorResolver,DeterministicBehaviorResolver>();
        services.AddSingleton<DeterministicEndingResolver>();
        services.AddSingleton<IEntityEndingResolver>(sp=>sp.GetRequiredService<DeterministicEndingResolver>());
        services.AddSingleton<IWorldEndingResolver>(sp=>sp.GetRequiredService<DeterministicEndingResolver>());
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
        options.Schema.For<EndingEvidenceView>().DatabaseSchemaName("hambaft");
        options.Projections.Add<SessionStateProjection>(ProjectionLifecycle.Inline);
        options.Projections.Add<PublicWorldProjection>(ProjectionLifecycle.Inline);
        options.Projections.Add<SessionExperienceProjection>(ProjectionLifecycle.Inline);
        options.Projections.Add<EndingEvidenceProjection>(ProjectionLifecycle.Inline);
        options.Events.AddEventTypes([typeof(SessionCreated),typeof(TeamAdded),typeof(WorldEntityCreated),typeof(EntityAssignedToTeam),typeof(InitialMetricSet),typeof(InitialMemoryAdded),typeof(InitialRelationshipSet),typeof(SessionStarted),typeof(SessionPaused),typeof(SessionResumed),typeof(SessionCancelled),typeof(NarrativeInitialized),typeof(StoryletAssigned),typeof(StoryChoiceSubmitted),typeof(NarrativeCheckpointResolved),typeof(MetricChanged),typeof(MetricSet),typeof(StoryMemoryAdded),typeof(StoryMemoryRemoved),typeof(RelationshipChanged),typeof(WorldNarrativePublished),typeof(ProposalSent),typeof(ProposalCountered),typeof(ProposalAccepted),typeof(ProposalRejected),typeof(ProposalCancelled),typeof(ProposalExpired),typeof(AgreementActivated),typeof(AgreementExecuted),typeof(AgreementFailed),typeof(AuthoredBehaviorActionSelected),typeof(ConsequenceScheduled),typeof(ConsequenceTriggered),typeof(ConsequenceCancelled),typeof(ConsequenceFailed),typeof(EntityEndingResolved),typeof(WorldEndingResolved),typeof(SessionCompleted)]);
    }
}
