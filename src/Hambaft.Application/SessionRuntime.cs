using Hambaft.Domain;

namespace Hambaft.Application;

public sealed class SessionRuntime
{
    private readonly ISessionStore store;
    private readonly IPairingCodeGenerator pairing;
    private readonly IStoryPackageLoader? packages;
    private readonly IStoryletSelector? selector;
    private readonly IEffectEngine? effects;

    public SessionRuntime(ISessionStore store,IPairingCodeGenerator pairing)
    { this.store=store; this.pairing=pairing; }
    public SessionRuntime(ISessionStore store,IPairingCodeGenerator pairing,IStoryPackageLoader packages,IStoryletSelector selector,IEffectEngine effects)
        : this(store,pairing) { this.packages=packages; this.selector=selector; this.effects=effects; }

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

    public async Task<CommandResult> ExecuteAsync(InitializeNarrative c,CancellationToken ct)
    {
        RequireStoryServices(); var state=await Load(c.SessionId,c.ExpectedVersion,ct); var package=await LoadLockedPackage(state,ct);
        if(state.Teams.Count<package.Manifest.MinimumTeams||state.Teams.Count>package.Manifest.MaximumTeams) throw new DomainException("Session Team count is outside the Story Package range.");
        ValidateRuntimeEntities(state,package);
        var appended=new List<IDomainEvent>();
        Add(state,appended,state.InitializeNarrative(package.Manifest.Id,package.Manifest.Version,package.ContentHash,package.Manifest.EntryCheckpointId,c.Context.For(c.SessionId)));
        InitializeMetrics(state,package,c.Context,appended);
        AssignCheckpoint(state,package,package.Manifest.EntryCheckpointId,c.Context,appended);
        await store.AppendAsync(c.SessionId,c.ExpectedVersion,appended,state,ct);
        return new(state.Id,state.StateVersion,nameof(NarrativeInitialized));
    }

    public async Task<CommandResult> ExecuteAsync(SubmitStoryChoice c,CancellationToken ct)
    {
        RequireStoryServices();
        if(c.Context.TeamId is not { } teamId) throw new DomainException("Authenticated Team identity is required.");
        var state=await Load(c.SessionId,c.ExpectedVersion,ct); var package=await LoadLockedPackage(state,ct);
        var assignment=state.StoryletAssignments.SingleOrDefault(x=>x.AssignmentId==c.AssignmentId)??throw new DomainException("Storylet Assignment does not exist.");
        var storylet=package.Storylets.SingleOrDefault(x=>x.Id==assignment.StoryletId)??throw new DomainException("Assigned Storylet no longer exists in the locked package.");
        var e=state.SubmitChoice(c.AssignmentId,teamId,c.ChoiceId,storylet.Choices.Select(x=>x.Id).ToList(),state.StateVersion+1,c.Context.For(c.SessionId));
        state.Apply(e); await store.AppendAsync(c.SessionId,c.ExpectedVersion,[e],state,ct); return Result(state,e);
    }

    public async Task<CommandResult> ExecuteAsync(ResolveNarrativeCheckpoint c,CancellationToken ct)
    {
        RequireStoryServices(); var state=await Load(c.SessionId,c.ExpectedVersion,ct); var package=await LoadLockedPackage(state,ct);
        var checkpoint=state.CurrentCheckpointId??throw new DomainException("Narrative is not initialized.");
        var current=state.StoryletAssignments.Where(x=>x.CheckpointId==checkpoint&&x.Status is StoryletAssignmentStatus.Assigned or StoryletAssignmentStatus.Responded).ToList();
        var missing=current.Where(x=>x.RequiredResponse&&x.Status!=StoryletAssignmentStatus.Responded).ToList();
        if(missing.Count>0) throw new DomainException("Required Storylet responses are still missing.");
        var submissions=state.SubmittedChoices.Where(s=>current.Any(a=>a.AssignmentId==s.AssignmentId)).Select(s=>new
        {
            Submission=s,
            Assignment=current.Single(a=>a.AssignmentId==s.AssignmentId),
            Storylet=package.Storylets.Single(st=>st.Id==current.Single(a=>a.AssignmentId==s.AssignmentId).StoryletId)
        }).OrderByDescending(x=>x.Storylet.Priority).ThenBy(x=>x.Assignment.AssignmentId).ThenBy(x=>x.Submission.TeamId).ToList();
        if(current.Any(x=>x.RequiredResponse)&&submissions.Count==0) throw new DomainException("Checkpoint has no submitted Choices to resolve.");
        var appended=new List<IDomainEvent>();
        foreach(var item in submissions)
        {
            var choice=item.Storylet.Choices.Single(ch=>ch.Id==item.Submission.ChoiceId);
            foreach(var effectId in choice.EffectIds)
            {
                var effect=package.Effects.Single(e=>e.Id==effectId);
                var metadata=c.Context.For(c.SessionId) with { CausationId=item.Submission.CommandId,TeamId=item.Submission.TeamId,CheckpointId=checkpoint,StoryletAssignmentId=item.Assignment.AssignmentId,ChoiceSubmissionId=item.Submission.CommandId };
                foreach(var @event in effects!.CreateEvents(effect,new(state,package,item.Assignment,item.Storylet,item.Submission,metadata,state.StateVersion+1))) Add(state,appended,@event);
            }
        }
        var nextCandidates=current.Select(a=>package.Storylets.Single(s=>s.Id==a.StoryletId).NextCheckpointId).Where(x=>x is not null).Distinct(StringComparer.Ordinal).ToList();
        if(nextCandidates.Count!=1) throw new DomainException("Checkpoint resolution requires exactly one deterministic next checkpoint.");
        var resolvedIds=current.Select(x=>x.AssignmentId).OrderBy(x=>x).ToList();
        Add(state,appended,state.ResolveCheckpoint(nextCandidates[0]!,resolvedIds,c.Context.For(c.SessionId)));
        AssignCheckpoint(state,package,nextCandidates[0]!,c.Context,appended);
        await store.AppendAsync(c.SessionId,c.ExpectedVersion,appended,state,ct);
        return new(state.Id,state.StateVersion,nameof(NarrativeCheckpointResolved));
    }

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

    private async Task<StoryPackage> LoadLockedPackage(StorySession state,CancellationToken ct)
    {
        var package=await packages!.LoadAsync(state.StoryPackageId,state.StoryVersion,ct);
        if(!string.Equals(package.ContentHash,state.ContentHash,StringComparison.Ordinal)) throw new StoryPackageHashMismatchException(state.ContentHash,package.ContentHash);
        return package;
    }
    private static void ValidateRuntimeEntities(StorySession state,StoryPackage package)
    {
        foreach(var definition in package.Entities)
        {
            var matches=state.Entities.Where(x=>x.DefinitionId==definition.Id).ToList();
            if(matches.Count!=1) throw new DomainException($"Session must contain exactly one Entity for definition '{definition.Id}'.");
            var entity=matches[0];
            if(definition.ControllerRequirement==ControllerRequirement.HumanTeam&&(entity.ControllerType!=ControllerType.HumanTeam||entity.ControlledByTeamId is null)) throw new DomainException($"Entity '{definition.Id}' requires a controlling human Team.");
            if(definition.ControllerRequirement==ControllerRequirement.System&&entity.ControllerType!=ControllerType.System) throw new DomainException($"Entity '{definition.Id}' requires System control.");
            if(definition.ControllerRequirement==ControllerRequirement.AuthoredBehavior&&entity.ControllerType!=ControllerType.AuthoredBehavior) throw new DomainException($"Entity '{definition.Id}' requires AuthoredBehavior control.");
        }
    }
    private static void InitializeMetrics(StorySession state,StoryPackage package,CommandContext context,List<IDomainEvent> events)
    {
        foreach(var metric in package.Metrics.Where(x=>x.Scope==MetricScope.World).OrderBy(x=>x.Key,StringComparer.Ordinal))
            Add(state,events,new MetricSet(MetricScope.World,state.Id,metric.Key,metric.DefaultValue,metric.DefaultValue,"$initialize",context.For(state.Id) with { CheckpointId=state.CurrentCheckpointId }));
        foreach(var team in state.Teams.OrderBy(x=>x.Id)) foreach(var metric in package.Metrics.Where(x=>x.Scope==MetricScope.Team).OrderBy(x=>x.Key,StringComparer.Ordinal))
            Add(state,events,new MetricSet(MetricScope.Team,team.Id,metric.Key,metric.DefaultValue,metric.DefaultValue,"$initialize",context.For(state.Id) with { TeamId=team.Id,CheckpointId=state.CurrentCheckpointId }));
        foreach(var entity in state.Entities.OrderBy(x=>x.Id))
        {
            var definition=package.Entities.Single(x=>x.Id==entity.DefinitionId);
            foreach(var metric in package.Metrics.Where(x=>x.Scope==MetricScope.Entity).OrderBy(x=>x.Key,StringComparer.Ordinal))
            {
                var value=definition.InitialMetrics.TryGetValue(metric.Key,out var initial)?Math.Clamp(initial,metric.Minimum,metric.Maximum):metric.DefaultValue;
                Add(state,events,new MetricSet(MetricScope.Entity,entity.Id,metric.Key,metric.DefaultValue,value,"$initialize",context.For(state.Id) with { TeamId=entity.ControlledByTeamId,CheckpointId=state.CurrentCheckpointId }));
            }
        }
        foreach(var source in state.Entities.OrderBy(x=>x.Id)) foreach(var target in state.Entities.Where(x=>x.Id!=source.Id).OrderBy(x=>x.Id)) foreach(var metric in package.Metrics.Where(x=>x.Scope==MetricScope.Relationship).OrderBy(x=>x.Key,StringComparer.Ordinal))
            Add(state,events,new RelationshipChanged(source.Id,target.Id,metric.Key,metric.DefaultValue,metric.DefaultValue,0,"$initialize",context.For(state.Id) with { TeamId=source.ControlledByTeamId,CheckpointId=state.CurrentCheckpointId }));
    }
    private void AssignCheckpoint(StorySession state,StoryPackage package,string checkpoint,CommandContext context,List<IDomainEvent> events)
    {
        AssignSelected(state,package,new(checkpoint,StoryletScope.WorldPublic,null,null,state.StateVersion),context,events);
        foreach(var team in state.Teams.OrderBy(x=>x.Id)) AssignSelected(state,package,new(checkpoint,StoryletScope.TeamPrivate,team.Id,team.ControlledEntityId,state.StateVersion),context,events);
        foreach(var entity in state.Entities.OrderBy(x=>x.Id)) AssignSelected(state,package,new(checkpoint,StoryletScope.EntityPrivate,entity.ControlledByTeamId,entity.Id,state.StateVersion),context,events);
    }
    private void AssignSelected(StorySession state,StoryPackage package,StoryletSelectionContext selection,CommandContext context,List<IDomainEvent> events)
    {
        var storylet=selector!.Select(package,state,selection); if(storylet is null) return;
        var id=DeterministicIds.Create(state.Id,selection.CheckpointId,storylet.Id,selection.TargetTeamId,selection.TargetEntityId,state.StateVersion+1);
        var assigned=state.AssignStorylet(id,storylet.Id,storylet.CheckpointId,storylet.Scope,selection.TargetTeamId,selection.TargetEntityId,storylet.RequiredResponse,state.StateVersion+1,context.For(state.Id)); Add(state,events,assigned);
        if(storylet.Scope==StoryletScope.WorldPublic)
            Add(state,events,new WorldNarrativePublished(storylet.Id,storylet.NarrativeRef,storylet.CheckpointId,state.CurrentNarrativeRevision+1,"$runtime",context.For(state.Id) with { CheckpointId=storylet.CheckpointId,StoryletAssignmentId=id }));
    }
    private static void Add(StorySession state,List<IDomainEvent> events,IDomainEvent @event) { events.Add(@event); state.Apply(@event); }
    private void RequireStoryServices() { if(packages is null||selector is null||effects is null) throw new InvalidOperationException("Story runtime services are not configured."); }
}
