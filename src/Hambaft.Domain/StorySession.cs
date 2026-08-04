namespace Hambaft.Domain;

public sealed class StorySession
{
    public Guid Id { get; private set; }
    public string StoryPackageId { get; private set; } = string.Empty;
    public string StoryVersion { get; private set; } = string.Empty;
    public string ContentHash { get; private set; } = string.Empty;
    public int Seed { get; private set; }
    public SessionStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public long StateVersion { get; private set; }
    public List<Team> Teams { get; private set; } = [];
    public List<WorldEntity> Entities { get; private set; } = [];
    public List<Metric> Metrics { get; private set; } = [];
    public List<StoryMemory> Memories { get; private set; } = [];
    public List<Relationship> Relationships { get; private set; } = [];

    public static StorySession From(IEnumerable<IDomainEvent> history)
    {
        var session = new StorySession();
        foreach (var @event in history) session.Apply(@event);
        return session;
    }

    public static SessionCreated Create(Guid id, string storyPackageId, string storyVersion, string contentHash, int seed, EventMetadata metadata)
    {
        if (id == Guid.Empty) throw new DomainException("Session ID is required.");
        Require(storyPackageId, nameof(storyPackageId));
        Require(storyVersion, nameof(storyVersion));
        Require(contentHash, nameof(contentHash));
        if (metadata.SessionId != id) throw new DomainException("Event metadata Session ID must match the Session.");
        return new(id, storyPackageId.Trim(), storyVersion.Trim(), contentHash.Trim(), seed, metadata);
    }

    public TeamAdded AddTeam(Guid teamId, string displayName, string pairingCodeHash, EventMetadata metadata)
    {
        EnsureSetup();
        if (teamId == Guid.Empty || Teams.Any(x => x.Id == teamId)) throw new DomainException("Team ID must be unique and non-empty.");
        Require(displayName, nameof(displayName)); Require(pairingCodeHash, nameof(pairingCodeHash));
        return new(teamId, displayName.Trim(), pairingCodeHash, metadata with { TeamId = teamId });
    }

    public WorldEntityCreated CreateWorldEntity(Guid entityId, string definitionId, string displayName, ControllerType controllerType, string? behaviorProfileId, EventMetadata metadata)
    {
        EnsureSetup();
        if (entityId == Guid.Empty || Entities.Any(x => x.Id == entityId)) throw new DomainException("Entity ID must be unique and non-empty.");
        Require(definitionId, nameof(definitionId)); Require(displayName, nameof(displayName));
        if (controllerType == ControllerType.AuthoredBehavior && string.IsNullOrWhiteSpace(behaviorProfileId)) throw new DomainException("AuthoredBehavior requires a Behavior Profile ID.");
        if (controllerType != ControllerType.AuthoredBehavior && behaviorProfileId is not null) throw new DomainException("Only AuthoredBehavior may have a Behavior Profile ID.");
        return new(entityId, definitionId.Trim(), displayName.Trim(), controllerType, behaviorProfileId?.Trim(), EntityStatus.Draft, metadata);
    }

    public EntityAssignedToTeam AssignEntityToTeam(Guid entityId, Guid teamId, EventMetadata metadata)
    {
        EnsureSetup();
        var entity = Entities.SingleOrDefault(x => x.Id == entityId) ?? throw new DomainException("Entity does not exist.");
        var team = Teams.SingleOrDefault(x => x.Id == teamId) ?? throw new DomainException("Team does not exist.");
        if (entity.ControllerType != ControllerType.HumanTeam) throw new DomainException("Only a HumanTeam Entity can be assigned to a Team.");
        if (entity.ControlledByTeamId is not null) throw new DomainException("Entity is already controlled by a Team.");
        if (team.ControlledEntityId is not null) throw new DomainException("Team already controls its primary Entity.");
        return new(entityId, teamId, metadata with { TeamId = teamId });
    }

    public InitialMetricSet SetInitialMetric(MetricScope scope, Guid scopeId, string metricKey, decimal numericValue, EventMetadata metadata)
    {
        EnsureInitialState(); ValidateScope(scope, scopeId);
        var key = DomainKeys.Normalize(metricKey, nameof(metricKey));
        if (Metrics.Any(x => x.Scope == scope && x.ScopeId == scopeId && x.MetricKey == key)) throw new DomainException("Metric key must be unique within its scope.");
        return new(scope, scopeId, key, numericValue, metadata);
    }

    public InitialMemoryAdded AddInitialMemory(MemoryScope scope, Guid scopeId, string key, string? json, MemoryVisibility visibility, EventMetadata metadata)
    {
        EnsureInitialState(); ValidateMemoryScope(scope, scopeId, visibility);
        var normalized = DomainKeys.Normalize(key, nameof(key));
        if (Memories.Any(x => x.Scope == scope && x.ScopeId == scopeId && x.Key == normalized)) throw new DomainException("Memory key must be unique within its scope.");
        return new(scope, scopeId, normalized, json, visibility, metadata);
    }

    public InitialRelationshipSet SetInitialRelationship(Guid sourceEntityId, Guid targetEntityId, string relationshipKey, decimal numericValue, EventMetadata metadata)
    {
        EnsureInitialState();
        if (sourceEntityId == targetEntityId) throw new DomainException("A relationship requires distinct source and target Entities.");
        if (!Entities.Any(x => x.Id == sourceEntityId) || !Entities.Any(x => x.Id == targetEntityId)) throw new DomainException("Relationship Entities must exist in the Session.");
        var key = DomainKeys.Normalize(relationshipKey, nameof(relationshipKey));
        if (Relationships.Any(x => x.SourceEntityId == sourceEntityId && x.TargetEntityId == targetEntityId && x.RelationshipKey == key)) throw new DomainException("Directional relationship key must be unique.");
        return new(sourceEntityId, targetEntityId, key, numericValue, metadata);
    }

    public SessionStarted Start(EventMetadata metadata)
    {
        if (Status is not (SessionStatus.Created or SessionStatus.Lobby)) throw InvalidTransition("start");
        if (Teams.Count == 0 || Entities.Count == 0) throw new DomainException("A Session requires at least one Team and Entity before start.");
        if (Entities.Any(x => x.ControllerType == ControllerType.HumanTeam && x.ControlledByTeamId is null)) throw new DomainException("Every HumanTeam Entity requires a controlling Team before start.");
        if (Entities.Any(x => x.ControllerType == ControllerType.AuthoredBehavior && string.IsNullOrWhiteSpace(x.BehaviorProfileId))) throw new DomainException("Every AuthoredBehavior Entity requires a Behavior Profile ID.");
        if (Entities.Any(x => x.ControllerType == ControllerType.System && x.ControlledByTeamId is not null)) throw new DomainException("System Entities cannot be assigned to Teams.");
        return new(metadata);
    }

    public SessionPaused Pause(EventMetadata metadata) => Status == SessionStatus.Running ? new(metadata) : throw InvalidTransition("pause");
    public SessionResumed Resume(EventMetadata metadata) => Status == SessionStatus.Paused ? new(metadata) : throw InvalidTransition("resume");
    public SessionCancelled Cancel(EventMetadata metadata) => Status is SessionStatus.Completed or SessionStatus.Cancelled ? throw InvalidTransition("cancel") : new(metadata);

    public void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case SessionCreated e: Id=e.Id; StoryPackageId=e.StoryPackageId; StoryVersion=e.StoryVersion; ContentHash=e.ContentHash; Seed=e.Seed; Status=SessionStatus.Created; CreatedAtUtc=e.Metadata.OccurredAtUtc; break;
            case TeamAdded e: Teams.Add(new(e.TeamId, Id, e.DisplayName, e.PairingCodeHash, null, e.Metadata.OccurredAtUtc)); if(Status==SessionStatus.Created) Status=SessionStatus.Lobby; break;
            case WorldEntityCreated e: Entities.Add(new(e.EntityId, Id, e.DefinitionId, e.DisplayName, e.ControllerType, null, e.BehaviorProfileId, e.Status)); break;
            case EntityAssignedToTeam e: ReplaceEntity(e.EntityId, x=>x with { ControlledByTeamId=e.TeamId, Status=EntityStatus.Active }); ReplaceTeam(e.TeamId, x=>x with { ControlledEntityId=e.EntityId }); break;
            case InitialMetricSet e: Metrics.Add(new(e.Scope,e.ScopeId,e.MetricKey,e.NumericValue)); break;
            case InitialMemoryAdded e: Memories.Add(new(e.Scope,e.ScopeId,e.Key,e.OptionalJsonValue,e.Visibility,e.Metadata.OccurredAtUtc)); break;
            case InitialRelationshipSet e: Relationships.Add(new(e.SourceEntityId,e.TargetEntityId,e.RelationshipKey,e.NumericValue)); break;
            case SessionStarted e: Status=SessionStatus.Running; StartedAtUtc=e.Metadata.OccurredAtUtc; Entities=Entities.Select(x=>x with { Status=EntityStatus.Active }).ToList(); break;
            case SessionPaused: Status=SessionStatus.Paused; break;
            case SessionResumed: Status=SessionStatus.Running; break;
            case SessionCancelled e: Status=SessionStatus.Cancelled; CompletedAtUtc=e.Metadata.OccurredAtUtc; break;
            default: throw new DomainException($"Unsupported event {@event.GetType().Name}.");
        }
        StateVersion++;
    }

    private void EnsureSetup() { if (Status is not (SessionStatus.Created or SessionStatus.Lobby)) throw new DomainException("Session setup is closed."); }
    private void EnsureInitialState() { if (Status is not (SessionStatus.Created or SessionStatus.Lobby)) throw new DomainException("Initial state can only be set before start."); }
    private void ValidateScope(MetricScope scope, Guid id)
    {
        var valid = scope switch { MetricScope.World => id == Id, MetricScope.Team => Teams.Any(x=>x.Id==id), MetricScope.Entity => Entities.Any(x=>x.Id==id), MetricScope.Relationship => id != Guid.Empty, _ => false };
        if (!valid) throw new DomainException("Metric scope does not belong to this Session.");
    }
    private void ValidateMemoryScope(MemoryScope scope, Guid id, MemoryVisibility visibility)
    {
        var valid = scope switch { MemoryScope.World => id==Id, MemoryScope.Team => Teams.Any(x=>x.Id==id), MemoryScope.Entity => Entities.Any(x=>x.Id==id), MemoryScope.Relationship => id!=Guid.Empty, _=>false };
        if (!valid) throw new DomainException("Memory scope does not belong to this Session.");
        if (visibility==MemoryVisibility.TeamPrivate && scope!=MemoryScope.Team) throw new DomainException("TeamPrivate memory requires Team scope.");
        if (visibility==MemoryVisibility.EntityPrivate && scope!=MemoryScope.Entity) throw new DomainException("EntityPrivate memory requires Entity scope.");
    }
    private void ReplaceEntity(Guid id, Func<WorldEntity,WorldEntity> change) { var i=Entities.FindIndex(x=>x.Id==id); Entities[i]=change(Entities[i]); }
    private void ReplaceTeam(Guid id, Func<Team,Team> change) { var i=Teams.FindIndex(x=>x.Id==id); Teams[i]=change(Teams[i]); }
    private static void Require(string value,string name) { if(string.IsNullOrWhiteSpace(value)) throw new DomainException($"{name} is required."); }
    private DomainException InvalidTransition(string action) => new($"Cannot {action} a Session in {Status} status.");
}
