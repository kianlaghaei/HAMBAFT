using Hambaft.Domain;

namespace Hambaft.Application;

public sealed record EffectExecutionContext(
    StorySession State,
    StoryPackage Package,
    StoryletAssignment? Assignment,
    StoryletDefinition? Storylet,
    SubmittedStoryChoice? Submission,
    EventMetadata Metadata,
    long NextStreamVersion,
    Guid? CurrentTeamId = null,
    Guid? CurrentEntityId = null);

public interface IEffectEngine
{
    IReadOnlyList<IDomainEvent> CreateEvents(EffectDefinition effect,EffectExecutionContext context);
}

public sealed class EffectEngine : IEffectEngine
{
    public IReadOnlyList<IDomainEvent> CreateEvents(EffectDefinition effect,EffectExecutionContext x)
    {
        var metadata=x.Metadata with
        {
            CheckpointId=x.Assignment?.CheckpointId??x.State.CurrentCheckpointId,
            StoryletAssignmentId=x.Assignment?.AssignmentId,
            ChoiceSubmissionId=x.Submission?.CommandId,
            TeamId=x.Assignment?.TargetTeamId??x.Metadata.TeamId
        };
        return effect.Type switch
        {
            EffectType.ChangeMetric=>[ChangeMetric(effect,x,metadata)],
            EffectType.SetMetric=>[SetMetric(effect,x,metadata)],
            EffectType.AddMemory=>AddMemory(effect,x,metadata),
            EffectType.RemoveMemory=>RemoveMemory(effect,x,metadata),
            EffectType.ChangeRelationship=>[ChangeRelationship(effect,x,metadata)],
            EffectType.AssignStorylet=>[AssignStorylet(effect,x,metadata)],
            EffectType.PublishWorldNarrative=>[Publish(effect,x,metadata)],
            EffectType.ScheduleConsequence=>ScheduleConsequence(effect,x,metadata),
            EffectType.CancelConsequence=>CancelConsequence(effect,x,metadata),
            _=>throw new DomainException($"Unsupported effect type {effect.Type}.")
        };
    }

    private static MetricChanged ChangeMetric(EffectDefinition e,EffectExecutionContext x,EventMetadata metadata)
    {
        var target=MetricTarget(e,x); var definition=MetricDefinitionFor(e,x,target.Scope); var previous=CurrentMetric(x.State,target.Scope,target.Id,e.MetricKey!,definition.DefaultValue); var delta=e.Value!.Value;
        var next=Math.Clamp(previous+delta,definition.Minimum,definition.Maximum);
        return new(target.Scope,target.Id,e.MetricKey!,previous,next,delta,e.Id,metadata);
    }
    private static MetricSet SetMetric(EffectDefinition e,EffectExecutionContext x,EventMetadata metadata)
    {
        var target=MetricTarget(e,x); var definition=MetricDefinitionFor(e,x,target.Scope); var previous=CurrentMetric(x.State,target.Scope,target.Id,e.MetricKey!,definition.DefaultValue);
        var next=Math.Clamp(e.Value!.Value,definition.Minimum,definition.Maximum);
        return new(target.Scope,target.Id,e.MetricKey!,previous,next,e.Id,metadata);
    }
    private static IReadOnlyList<IDomainEvent> AddMemory(EffectDefinition e,EffectExecutionContext x,EventMetadata metadata)
    {
        var target=MemoryTarget(e,x); var key=DomainKeys.Normalize(e.MemoryKey!,nameof(e.MemoryKey));
        if(x.State.Memories.Any(m=>m.Scope==target.Scope&&m.ScopeId==target.Id&&m.Key==key)) return [];
        var visibility=e.Visibility??MemoryVisibility.SystemOnly;
        if(visibility==MemoryVisibility.TeamPrivate&&target.Scope!=MemoryScope.Team) throw new DomainException("TeamPrivate memory requires CurrentTeam scope.");
        if(visibility==MemoryVisibility.EntityPrivate&&target.Scope!=MemoryScope.Entity) throw new DomainException("EntityPrivate memory requires an Entity scope.");
        return [new StoryMemoryAdded(target.Scope,target.Id,key,e.JsonValue,visibility,e.Id,metadata)];
    }
    private static IReadOnlyList<IDomainEvent> RemoveMemory(EffectDefinition e,EffectExecutionContext x,EventMetadata metadata)
    {
        var target=MemoryTarget(e,x); var key=DomainKeys.Normalize(e.MemoryKey!,nameof(e.MemoryKey));
        return x.State.Memories.Any(m=>m.Scope==target.Scope&&m.ScopeId==target.Id&&m.Key==key)?[new StoryMemoryRemoved(target.Scope,target.Id,key,e.Id,metadata)]:[];
    }
    private static RelationshipChanged ChangeRelationship(EffectDefinition e,EffectExecutionContext x,EventMetadata metadata)
    {
        var source=CurrentEntity(x); var target=x.State.Entities.SingleOrDefault(entity=>entity.DefinitionId==e.TargetEntityDefinitionId)??throw new DomainException("Relationship target Entity cannot be resolved.");
        if(source.Id==target.Id) throw new DomainException("Relationship source and target must differ.");
        var definition=x.Package.Metrics.SingleOrDefault(m=>m.Scope==MetricScope.Relationship&&m.Key==e.RelationshipKey)??throw new DomainException("Relationship metric is not defined.");
        var previous=x.State.Relationships.SingleOrDefault(r=>r.SourceEntityId==source.Id&&r.TargetEntityId==target.Id&&r.RelationshipKey==e.RelationshipKey)?.NumericValue??definition.DefaultValue;
        var delta=e.Value!.Value; var next=Math.Clamp(previous+delta,definition.Minimum,definition.Maximum);
        return new(source.Id,target.Id,e.RelationshipKey!,previous,next,delta,e.Id,metadata);
    }
    private static StoryletAssigned AssignStorylet(EffectDefinition e,EffectExecutionContext x,EventMetadata metadata)
    {
        var storylet=x.Package.Storylets.SingleOrDefault(s=>s.Id==e.StoryletId)??throw new DomainException("Assigned Storylet is not defined.");
        var (team,entity)=ResolveStoryletTarget(storylet,x);
        var id=DeterministicIds.Create(x.State.Id,storylet.CheckpointId,storylet.Id,team,entity,x.NextStreamVersion,e.Id);
        return x.State.AssignStorylet(id,storylet.Id,storylet.CheckpointId,storylet.Scope,team,entity,storylet.RequiredResponse,x.NextStreamVersion,metadata);
    }
    private static WorldNarrativePublished Publish(EffectDefinition e,EffectExecutionContext x,EventMetadata metadata)
    {
        var storylet=x.Package.Storylets.SingleOrDefault(s=>s.Id==e.StoryletId)??throw new DomainException("Published Storylet is not defined.");
        if(storylet.Scope!=StoryletScope.WorldPublic) throw new DomainException("A private Storylet cannot be published as World narrative.");
        return new(storylet.Id,storylet.NarrativeRef,storylet.CheckpointId,x.State.CurrentNarrativeRevision+1,e.Id,metadata);
    }
    private static IReadOnlyList<IDomainEvent> ScheduleConsequence(EffectDefinition e,EffectExecutionContext x,EventMetadata metadata)
    {
        var definition=x.Package.ConsequenceDefinitions.SingleOrDefault(c=>c.Id==e.ConsequenceDefinitionId)??throw new DomainException("Scheduled Consequence definition is not defined.");
        var sourceTeam=x.CurrentTeamId??x.Assignment?.TargetTeamId??metadata.TeamId;
        var sourceEntity=x.CurrentEntityId??x.Assignment?.TargetEntityId??(sourceTeam is { } team?x.State.Teams.SingleOrDefault(t=>t.Id==team)?.ControlledEntityId:null);
        if(definition.RepeatPolicy==RepeatPolicy.OncePerSession&&x.State.ScheduledConsequences.Any(c=>c.DefinitionId==definition.Id))return [];
        if(definition.RepeatPolicy==RepeatPolicy.OncePerCheckpoint&&x.State.ScheduledConsequences.Any(c=>c.DefinitionId==definition.Id&&c.ScheduledAtCheckpointId==x.State.CurrentCheckpointId))return [];
        var due=definition.Trigger.Type switch
        {
            ConsequenceTriggerType.AfterCheckpointCount=>SessionRuntime.ResolveValidity(x.Package,x.State.CurrentCheckpointId!,new(ProposalValidityType.ValidForCheckpointCount,CheckpointCount:(definition.Trigger.CheckpointCount??throw new DomainException("AfterCheckpointCount requires checkpointCount."))+1)),
            ConsequenceTriggerType.AtCheckpoint=>definition.Trigger.CheckpointId,
            _=>null
        };
        var id=DeterministicIds.Create(x.State.Id,definition.Id,metadata.CommandId,x.State.CurrentCheckpointId,x.State.StateVersion+1);
        return [new ConsequenceScheduled(id,definition.Id,metadata.CausationId,sourceTeam,sourceEntity,x.State.CurrentCheckpointId!,due,definition.Trigger.Type,definition.Visibility,metadata)];
    }
    private static IReadOnlyList<IDomainEvent> CancelConsequence(EffectDefinition e,EffectExecutionContext x,EventMetadata metadata)
    {
        var candidates=x.State.ScheduledConsequences.Where(c=>c.Status==ScheduledConsequenceStatus.Pending&&(e.ConsequenceDefinitionId is null||c.DefinitionId==e.ConsequenceDefinitionId)).OrderBy(c=>c.ScheduledConsequenceId).ToList();
        return candidates.Select(c=>(IDomainEvent)new ConsequenceCancelled(c.ScheduledConsequenceId,c.DefinitionId,c.SourceEventId,c.SourceTeamId,c.SourceEntityId,metadata)).ToList();
    }
    private static (MetricScope Scope,Guid Id) MetricTarget(EffectDefinition e,EffectExecutionContext x)=>e.Scope switch
    {
        ConditionTargetScope.World=>(MetricScope.World,x.State.Id),
        ConditionTargetScope.CurrentTeam=>(MetricScope.Team,CurrentTeam(x).Id),
        ConditionTargetScope.CurrentEntity=>(MetricScope.Entity,CurrentEntity(x).Id),
        ConditionTargetScope.ExplicitEntityDefinition=>(MetricScope.Entity,ExplicitEntity(e.TargetEntityDefinitionId,x).Id),
        _=>throw new DomainException("Metric effect target scope cannot be resolved.")
    };
    private static (MemoryScope Scope,Guid Id) MemoryTarget(EffectDefinition e,EffectExecutionContext x)=>e.Scope switch
    {
        ConditionTargetScope.World=>(MemoryScope.World,x.State.Id),
        ConditionTargetScope.CurrentTeam=>(MemoryScope.Team,CurrentTeam(x).Id),
        ConditionTargetScope.CurrentEntity=>(MemoryScope.Entity,CurrentEntity(x).Id),
        ConditionTargetScope.ExplicitEntityDefinition=>(MemoryScope.Entity,ExplicitEntity(e.TargetEntityDefinitionId,x).Id),
        _=>throw new DomainException("Memory effect target scope cannot be resolved.")
    };
    private static MetricDefinition MetricDefinitionFor(EffectDefinition e,EffectExecutionContext x,MetricScope scope)=>x.Package.Metrics.SingleOrDefault(m=>m.Scope==scope&&m.Key==e.MetricKey)??throw new DomainException("Metric effect references an unknown or incompatible metric.");
    private static decimal CurrentMetric(StorySession state,MetricScope scope,Guid id,string key,decimal fallback)=>state.Metrics.SingleOrDefault(m=>m.Scope==scope&&m.ScopeId==id&&m.MetricKey==key)?.NumericValue??fallback;
    private static Team CurrentTeam(EffectExecutionContext x)=>(x.CurrentTeamId??x.Assignment?.TargetTeamId) is { } id?x.State.Teams.Single(t=>t.Id==id):throw new DomainException("Effect requires a current Team.");
    private static WorldEntity CurrentEntity(EffectExecutionContext x)
    {
        var id=x.CurrentEntityId??x.Assignment?.TargetEntityId??((x.CurrentTeamId??x.Assignment?.TargetTeamId) is { } team?x.State.Teams.Single(t=>t.Id==team).ControlledEntityId:null);
        return id is { } entityId?x.State.Entities.Single(e=>e.Id==entityId):throw new DomainException("Effect requires a current Entity.");
    }
    private static WorldEntity ExplicitEntity(string? definitionId,EffectExecutionContext x)=>x.State.Entities.SingleOrDefault(e=>e.DefinitionId==definitionId)??throw new DomainException("Explicit Entity definition cannot be resolved.");
    private static (Guid? Team,Guid? Entity) ResolveStoryletTarget(StoryletDefinition storylet,EffectExecutionContext x)=>storylet.TargetSelector.Type switch
    {
        TargetSelectorType.World=>(null,null),
        TargetSelectorType.AllTeams=>(CurrentTeam(x).Id,CurrentTeam(x).ControlledEntityId),
        TargetSelectorType.TeamControllingEntityDefinition=>x.State.Entities.SingleOrDefault(entity=>entity.DefinitionId==storylet.TargetSelector.EntityDefinitionId&&entity.ControlledByTeamId is not null) is { } controlled?(controlled.ControlledByTeamId,controlled.Id):throw new DomainException("Assigned Storylet target Team cannot be resolved."),
        TargetSelectorType.EntityDefinition=>ExplicitEntity(storylet.TargetSelector.EntityDefinitionId,x) is { } entity?(entity.ControlledByTeamId,entity.Id):throw new DomainException("Assigned Storylet target Entity cannot be resolved."),
        _=>throw new DomainException("Assigned Storylet target selector is invalid.")
    };
}
