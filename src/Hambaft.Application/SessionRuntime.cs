using Hambaft.Domain;

namespace Hambaft.Application;

public sealed class SessionRuntime(ISessionStore store, IPairingCodeGenerator pairing)
{
    public async Task<CommandResult> ExecuteAsync(CreateSession command, CancellationToken ct)
    {
        if(command.ExpectedVersion!=0) throw new ConcurrencyConflictException(command.SessionId,command.ExpectedVersion,0);
        if(await store.LoadAsync(command.SessionId,ct) is not null) throw new ConcurrencyConflictException(command.SessionId,0,1);
        var e=StorySession.Create(command.SessionId,command.StoryPackageId,command.StoryVersion,command.ContentHash,command.Seed,command.Context.For(command.SessionId));
        var state=StorySession.From([e]); await store.AppendAsync(command.SessionId,0,[e],state,ct); return Result(state,e);
    }
    public async Task<CommandResult> ExecuteAsync(AddTeam command,CancellationToken ct)
    {
        var state=await Load(command.SessionId,command.ExpectedVersion,ct); var code=pairing.Generate(); var e=state.AddTeam(command.TeamId,command.DisplayName,code.Hash,command.Context.For(command.SessionId)); state.Apply(e); await store.AppendAsync(command.SessionId,command.ExpectedVersion,[e],state,ct); return Result(state,e,code.RawCode);
    }
    public Task<CommandResult> ExecuteAsync(CreateWorldEntity c,CancellationToken ct)=>Change(c.SessionId,c.ExpectedVersion,c.Context,s=>s.CreateWorldEntity(c.EntityId,c.DefinitionId,c.DisplayName,c.ControllerType,c.BehaviorProfileId,c.Context.For(c.SessionId)),ct);
    public Task<CommandResult> ExecuteAsync(AssignEntityToTeam c,CancellationToken ct)=>Change(c.SessionId,c.ExpectedVersion,c.Context,s=>s.AssignEntityToTeam(c.EntityId,c.TeamId,c.Context.For(c.SessionId)),ct);
    public Task<CommandResult> ExecuteAsync(SetInitialMetric c,CancellationToken ct)=>Change(c.SessionId,c.ExpectedVersion,c.Context,s=>s.SetInitialMetric(c.Scope,c.ScopeId,c.MetricKey,c.NumericValue,c.Context.For(c.SessionId)),ct);
    public Task<CommandResult> ExecuteAsync(AddInitialMemory c,CancellationToken ct)=>Change(c.SessionId,c.ExpectedVersion,c.Context,s=>s.AddInitialMemory(c.Scope,c.ScopeId,c.Key,c.OptionalJsonValue,c.Visibility,c.Context.For(c.SessionId)),ct);
    public Task<CommandResult> ExecuteAsync(SetInitialRelationship c,CancellationToken ct)=>Change(c.SessionId,c.ExpectedVersion,c.Context,s=>s.SetInitialRelationship(c.SourceEntityId,c.TargetEntityId,c.RelationshipKey,c.NumericValue,c.Context.For(c.SessionId)),ct);
    public Task<CommandResult> ExecuteAsync(StartSession c,CancellationToken ct)=>Change(c.SessionId,c.ExpectedVersion,c.Context,s=>s.Start(c.Context.For(c.SessionId)),ct);
    public Task<CommandResult> ExecuteAsync(PauseSession c,CancellationToken ct)=>Change(c.SessionId,c.ExpectedVersion,c.Context,s=>s.Pause(c.Context.For(c.SessionId)),ct);
    public Task<CommandResult> ExecuteAsync(ResumeSession c,CancellationToken ct)=>Change(c.SessionId,c.ExpectedVersion,c.Context,s=>s.Resume(c.Context.For(c.SessionId)),ct);
    public Task<CommandResult> ExecuteAsync(CancelSession c,CancellationToken ct)=>Change(c.SessionId,c.ExpectedVersion,c.Context,s=>s.Cancel(c.Context.For(c.SessionId)),ct);

    public Task<CommandResult> CreateEntityAsync(Guid sessionId,Guid entityId,string definitionId,string displayName,string controllerType,string? behaviorProfileId,long expectedVersion,CommandContext context,CancellationToken ct)=>ExecuteAsync(CommandFactory.CreateEntity(sessionId,entityId,definitionId,displayName,controllerType,behaviorProfileId,expectedVersion,context),ct);
    public Task<CommandResult> SetMetricAsync(Guid sessionId,string scope,Guid scopeId,string key,decimal value,long expectedVersion,CommandContext context,CancellationToken ct)=>ExecuteAsync(CommandFactory.SetMetric(sessionId,scope,scopeId,key,value,expectedVersion,context),ct);
    public Task<CommandResult> AddMemoryAsync(Guid sessionId,string scope,Guid scopeId,string key,string? json,string visibility,long expectedVersion,CommandContext context,CancellationToken ct)=>ExecuteAsync(CommandFactory.AddMemory(sessionId,scope,scopeId,key,json,visibility,expectedVersion,context),ct);

    public Task<SessionStateView?> GetSessionAsync(Guid id,CancellationToken ct)=>store.LoadSessionViewAsync(id,ct);
    public Task<PublicWorldView?> GetPublicAsync(Guid id,CancellationToken ct)=>store.LoadPublicViewAsync(id,ct);
    public Task<TeamExperienceView?> GetTeamAsync(Guid teamId,CancellationToken ct)=>store.LoadTeamViewAsync(teamId,ct);

    private async Task<CommandResult> Change(Guid id,long version,CommandContext _,Func<StorySession,IDomainEvent> decide,CancellationToken ct)
    {
        var state=await Load(id,version,ct); var e=decide(state); state.Apply(e); await store.AppendAsync(id,version,[e],state,ct); return Result(state,e);
    }
    private async Task<StorySession> Load(Guid id,long expected,CancellationToken ct)
    {
        var state=await store.LoadAsync(id,ct)??throw new SessionNotFoundException(id);
        if(state.StateVersion!=expected) throw new ConcurrencyConflictException(id,expected,state.StateVersion);
        return state;
    }
    private static CommandResult Result(StorySession state,IDomainEvent e,string? code=null)=>new(state.Id,state.StateVersion,e.GetType().Name,code);
}
