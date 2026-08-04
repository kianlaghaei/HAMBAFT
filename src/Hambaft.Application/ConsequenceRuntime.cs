using Hambaft.Domain;

namespace Hambaft.Application;

public sealed partial class SessionRuntime
{
    public async Task<CommandResult> ExecuteAsync(ScheduleConsequence c,CancellationToken ct)
    {
        RequireStoryServices();var state=await Load(c.SessionId,c.ExpectedVersion,ct);var package=await LoadLockedPackage(state,ct);var definition=package.ConsequenceDefinitions.SingleOrDefault(x=>x.Id==c.DefinitionId)??throw new DomainException("Consequence definition does not exist.");
        var due=DueCheckpoint(package,state.CurrentCheckpointId!,definition);var id=DeterministicIds.Create(state.Id,definition.Id,c.SourceEventId,state.StateVersion+1);var scheduled=new ConsequenceScheduled(id,definition.Id,c.SourceEventId,c.SourceTeamId,c.SourceEntityId,state.CurrentCheckpointId!,due,definition.Trigger.Type,definition.Visibility,c.Context.For(state.Id) with { CheckpointId=state.CurrentCheckpointId });
        state.Apply(scheduled);await store.AppendAsync(c.SessionId,c.ExpectedVersion,[scheduled],state,ct);return new(state.Id,state.StateVersion,nameof(ConsequenceScheduled),ResourceId:id,AffectedTeamIds:c.SourceTeamId is { } team?[team]:[],IsPublic:definition.Visibility==ConsequenceVisibility.Public,EmittedEventTypes:[nameof(ConsequenceScheduled)]);
    }

    public async Task<CommandResult> ExecuteAsync(CancelConsequence c,CancellationToken ct)
    {
        var state=await Load(c.SessionId,c.ExpectedVersion,ct);var value=state.ScheduledConsequences.SingleOrDefault(x=>x.ScheduledConsequenceId==c.ScheduledConsequenceId)??throw new DomainException("Scheduled Consequence does not exist.");if(value.Status!=ScheduledConsequenceStatus.Pending)throw new DomainException("Only a pending Consequence may be cancelled.");
        var cancelled=new ConsequenceCancelled(value.ScheduledConsequenceId,value.DefinitionId,value.SourceEventId,value.SourceTeamId,value.SourceEntityId,c.Context.For(state.Id) with { CheckpointId=state.CurrentCheckpointId });state.Apply(cancelled);await store.AppendAsync(c.SessionId,c.ExpectedVersion,[cancelled],state,ct);return new(state.Id,state.StateVersion,nameof(ConsequenceCancelled),ResourceId:value.ScheduledConsequenceId,AffectedTeamIds:value.SourceTeamId is { } team?[team]:[],IsPublic:value.Visibility==ConsequenceVisibility.Public,EmittedEventTypes:[nameof(ConsequenceCancelled)]);
    }

    public async Task<CommandResult> ExecuteAsync(ResolveDueConsequences c,CancellationToken ct)
    {
        RequireStoryServices();var state=await Load(c.SessionId,c.ExpectedVersion,ct);var package=await LoadLockedPackage(state,ct);var appended=new List<IDomainEvent>();ResolveConsequenceEvents(state,package,c.Context,appended);
        if(appended.Count>0)await store.AppendAsync(c.SessionId,c.ExpectedVersion,appended,state,ct);return new(state.Id,state.StateVersion,appended.Count==0?"NoDueConsequences":nameof(ConsequenceTriggered),EmittedEventTypes:appended.Select(x=>x.GetType().Name).ToList());
    }

    private void ResolveConsequenceEvents(StorySession state,StoryPackage package,CommandContext context,List<IDomainEvent> appended)
    {
        foreach(var scheduled in state.ScheduledConsequences.Where(x=>x.Status==ScheduledConsequenceStatus.Pending).OrderBy(x=>x.ScheduledConsequenceId).ToList())
        {
            var definition=package.ConsequenceDefinitions.Single(x=>x.Id==scheduled.DefinitionId);if(!IsDue(scheduled,definition,state))continue;
            var conditionContext=new ConditionEvaluationContext(state,package,scheduled.SourceTeamId,scheduled.SourceEntityId);
            if(!new ConditionEngine().EvaluateAll(definition.Conditions,conditionContext,false).Result)continue;
            var metadata=context.For(state.Id) with { TeamId=scheduled.SourceTeamId,CheckpointId=state.CurrentCheckpointId,CausationId=scheduled.SourceEventId };
            Add(state,appended,new ConsequenceTriggered(scheduled.ScheduledConsequenceId,scheduled.DefinitionId,scheduled.SourceEventId,scheduled.SourceTeamId,scheduled.SourceEntityId,state.CurrentCheckpointId!,scheduled.Visibility,metadata));
            foreach(var effectId in definition.EffectIds)
            {
                var effect=package.Effects.Single(x=>x.Id==effectId);foreach(var @event in effects!.CreateEvents(effect,new(state,package,null,null,null,metadata,state.StateVersion+1,scheduled.SourceTeamId,scheduled.SourceEntityId)))Add(state,appended,@event);
            }
            if(definition.NarrativeRef is { } narrative&&definition.Visibility==ConsequenceVisibility.Public)
            {
                if(!package.Narrative.TryGetValue(package.Manifest.DefaultLocale,out var locale)||!locale.ContainsKey(narrative))throw new DomainException($"Consequence narrative '{narrative}' is missing.");
                Add(state,appended,new WorldNarrativePublished($"consequence:{definition.Id}",narrative,state.CurrentCheckpointId!,state.CurrentNarrativeRevision+1,"$consequence",metadata));
            }
        }
    }

    private static bool IsDue(ScheduledConsequence scheduled,ConsequenceDefinition definition,StorySession state)=>definition.Trigger.Type switch
    {
        ConsequenceTriggerType.AfterCheckpointCount or ConsequenceTriggerType.AtCheckpoint=>scheduled.DueCheckpointId==state.CurrentCheckpointId,
        ConsequenceTriggerType.WhenAgreementExecuted=>state.Agreements.Any(x=>x.Status==AgreementStatus.Executed&&(definition.Trigger.AgreementInteractionTypeId is null||x.InteractionTypeId==definition.Trigger.AgreementInteractionTypeId)),
        ConsequenceTriggerType.WhenMemoryExistsAtResolution=>MemoryExists(scheduled,definition,state),
        _=>false
    };
    private static bool MemoryExists(ScheduledConsequence scheduled,ConsequenceDefinition definition,StorySession state)
    {
        var key=definition.Trigger.MemoryKey;if(string.IsNullOrWhiteSpace(key))return false;
        return definition.Trigger.MemoryScope switch
        {
            ConditionTargetScope.World=>state.Memories.Any(x=>x.Scope==MemoryScope.World&&x.ScopeId==state.Id&&x.Key==key),
            ConditionTargetScope.CurrentTeam=>scheduled.SourceTeamId is { } team&&state.Memories.Any(x=>x.Scope==MemoryScope.Team&&x.ScopeId==team&&x.Key==key),
            ConditionTargetScope.CurrentEntity=>scheduled.SourceEntityId is { } entity&&state.Memories.Any(x=>x.Scope==MemoryScope.Entity&&x.ScopeId==entity&&x.Key==key),
            _=>false
        };
    }
    private static string? DueCheckpoint(StoryPackage package,string current,ConsequenceDefinition definition)=>definition.Trigger.Type switch
    {
        ConsequenceTriggerType.AfterCheckpointCount=>ResolveValidity(package,current,new(ProposalValidityType.ValidForCheckpointCount,CheckpointCount:(definition.Trigger.CheckpointCount??throw new DomainException("AfterCheckpointCount requires checkpointCount."))+1)),
        ConsequenceTriggerType.AtCheckpoint=>definition.Trigger.CheckpointId,
        _=>null
    };
}
