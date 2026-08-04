using System.Text.Json;
using Hambaft.Domain;

namespace Hambaft.Application;

public interface IInteractionTermsValidator
{
    void Validate(InteractionTermSchemaDefinition schema,JsonElement payload,IReadOnlyDictionary<string,decimal>? strictnessValues=null);
}

public sealed class InteractionTermsValidator : IInteractionTermsValidator
{
    public void Validate(InteractionTermSchemaDefinition schema,JsonElement payload,IReadOnlyDictionary<string,decimal>? strictnessValues=null)=>ValidateNode(schema,payload,"terms",strictnessValues??new Dictionary<string,decimal>());

    private static void ValidateNode(InteractionTermSchemaDefinition schema,JsonElement value,string path,IReadOnlyDictionary<string,decimal> strictness)
    {
        switch(schema.Type)
        {
            case InteractionTermType.Numeric:
                if(value.ValueKind!=JsonValueKind.Number||!value.TryGetDecimal(out var number)) throw new DomainException($"{path} must be Numeric.");
                var multiplier=schema.Name is not null&&strictness.TryGetValue($"{schema.Name}.minimumMultiplier",out var authored)?authored:1m;var effectiveMinimum=schema.Minimum*multiplier;
                if(effectiveMinimum is { } minimum&&number<minimum||schema.Maximum is { } maximum&&number>maximum) throw new DomainException($"{path} is outside its authored numeric range.");
                break;
            case InteractionTermType.Boolean:
                if(value.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw new DomainException($"{path} must be Boolean.");
                break;
            case InteractionTermType.ShortText:
                if(value.ValueKind!=JsonValueKind.String) throw new DomainException($"{path} must be ShortText.");
                if((value.GetString()?.Length??0)>(schema.MaximumLength??200)) throw new DomainException($"{path} exceeds its authored maximum length.");
                break;
            case InteractionTermType.Compound:
                if(value.ValueKind!=JsonValueKind.Object) throw new DomainException($"{path} must be Compound.");
                var fields=schema.Fields??[];
                var names=fields.Select(x=>x.Name).Where(x=>x is not null).ToHashSet(StringComparer.Ordinal);
                foreach(var property in value.EnumerateObject()) if(!names.Contains(property.Name)) throw new DomainException($"{path}.{property.Name} is not authored in the terms schema.");
                foreach(var field in fields)
                {
                    if(string.IsNullOrWhiteSpace(field.Name)) throw new DomainException("Compound term fields require names.");
                    if(!value.TryGetProperty(field.Name,out var child)) { if(field.Required) throw new DomainException($"{path}.{field.Name} is required."); continue; }
                    ValidateNode(field,child,$"{path}.{field.Name}",strictness);
                }
                break;
            default: throw new DomainException("Unsupported interaction term type.");
        }
    }
}

public sealed partial class SessionRuntime
{
    public async Task<CommandResult> ExecuteAsync(SendProposal c,CancellationToken ct)
    {
        RequireStoryServices(); var state=await Load(c.SessionId,c.ExpectedVersion,ct); var package=await LoadLockedPackage(state,ct);
        var sender=c.Context.TeamId is { } teamId?state.Teams.SingleOrDefault(x=>x.Id==teamId)??throw new DomainException("Authenticated Team does not belong to the Session."):throw new DomainException("Authenticated Team identity is required.");
        var receiver=state.Teams.SingleOrDefault(x=>x.Id==c.ReceiverTeamId)??throw new DomainException("Receiver Team does not belong to the Session.");
        var definition=package.InteractionDefinitions.SingleOrDefault(x=>x.Id==c.InteractionTypeId)??throw new DomainException("Interaction type does not exist in the locked Story Package.");
        if(!TeamMatchesAny(sender,state,definition.AllowedSenderSelectors)) throw new DomainException("Sender is not eligible for this interaction type.");
        if(!TeamMatchesAny(receiver,state,definition.AllowedReceiverSelectors)) throw new DomainException("Receiver is not eligible for this interaction type.");
        interactionTerms.Validate(definition.TermsSchema,c.TermsPayload,package.DifficultyDefinitions.SingleOrDefault(x=>x.Id==state.DifficultyId)?.ProposalStrictnessValues);
        var deadline=ResolveValidity(package,state.CurrentCheckpointId!,c.Validity??definition.DefaultValidity);
        var sent=state.SendProposal(c.ProposalId,definition.Id,receiver.Id,c.TermsPayload,definition.OfferedEffectIds,definition.RequestedEffectIds,deadline,state.StateVersion+1,c.Context.For(c.SessionId));
        state.Apply(sent); await store.AppendAsync(c.SessionId,c.ExpectedVersion,[sent],state,ct);
        return new(state.Id,state.StateVersion,nameof(ProposalSent),ResourceId:c.ProposalId,AffectedTeamIds:[sender.Id,receiver.Id],EmittedEventTypes:[nameof(ProposalSent)]);
    }

    public async Task<CommandResult> ExecuteAsync(CounterProposal c,CancellationToken ct)
    {
        RequireStoryServices(); var state=await Load(c.SessionId,c.ExpectedVersion,ct); var package=await LoadLockedPackage(state,ct);
        var proposal=state.Proposals.SingleOrDefault(x=>x.ProposalId==c.ProposalId)??throw new DomainException("Proposal does not exist.");
        var definition=package.InteractionDefinitions.Single(x=>x.Id==proposal.InteractionTypeId); interactionTerms.Validate(definition.TermsSchema,c.TermsPayload,package.DifficultyDefinitions.SingleOrDefault(x=>x.Id==state.DifficultyId)?.ProposalStrictnessValues);
        var countered=state.CounterProposal(c.ProposalId,c.ExpectedRevisionNumber,c.TermsPayload,state.StateVersion+1,c.Context.For(c.SessionId));
        state.Apply(countered); await store.AppendAsync(c.SessionId,c.ExpectedVersion,[countered],state,ct);
        return InteractionResult(state,c.ProposalId,nameof(ProposalCountered),proposal,[nameof(ProposalCountered)]);
    }

    public async Task<CommandResult> ExecuteAsync(AcceptProposal c,CancellationToken ct)
    {
        RequireStoryServices(); var state=await Load(c.SessionId,c.ExpectedVersion,ct); var package=await LoadLockedPackage(state,ct);
        var proposal=state.Proposals.SingleOrDefault(x=>x.ProposalId==c.ProposalId)??throw new DomainException("Proposal does not exist.");
        var definition=package.InteractionDefinitions.Single(x=>x.Id==proposal.InteractionTypeId);
        var revision=state.ProposalRevisions.Single(x=>x.ProposalId==proposal.ProposalId&&x.RevisionNumber==proposal.CurrentRevisionNumber); interactionTerms.Validate(definition.TermsSchema,revision.TermsPayload,package.DifficultyDefinitions.SingleOrDefault(x=>x.Id==state.DifficultyId)?.ProposalStrictnessValues);
        var agreementId=DeterministicIds.Create(state.Id,c.ProposalId,c.ExpectedRevisionNumber,"agreement"); var appended=new List<IDomainEvent>();
        Add(state,appended,state.AcceptProposal(c.ProposalId,c.ExpectedRevisionNumber,agreementId,c.Context.For(c.SessionId)));
        Add(state,appended,state.ActivateAgreement(c.ProposalId,agreementId,definition.AgreementVisibility,c.Context.For(c.SessionId)));
        if(definition.ExecutionMode==InteractionExecutionMode.ImmediateOnAcceptance) ExecuteAgreementEvents(state,package,agreementId,c.Context,appended);
        await store.AppendAsync(c.SessionId,c.ExpectedVersion,appended,state,ct);
        return new(state.Id,state.StateVersion,nameof(ProposalAccepted),ResourceId:agreementId,AffectedTeamIds:[proposal.SenderTeamId,proposal.ReceiverTeamId],IsPublic:definition.AgreementVisibility==AgreementVisibility.Public,EmittedEventTypes:appended.Select(x=>x.GetType().Name).ToList());
    }

    public async Task<CommandResult> ExecuteAsync(RejectProposal c,CancellationToken ct)
    {
        var state=await Load(c.SessionId,c.ExpectedVersion,ct); var proposal=state.Proposals.SingleOrDefault(x=>x.ProposalId==c.ProposalId)??throw new DomainException("Proposal does not exist.");
        var rejected=state.RejectProposal(c.ProposalId,c.ExpectedRevisionNumber,c.Context.For(c.SessionId));state.Apply(rejected);await store.AppendAsync(c.SessionId,c.ExpectedVersion,[rejected],state,ct);
        return InteractionResult(state,c.ProposalId,nameof(ProposalRejected),proposal,[nameof(ProposalRejected)]);
    }

    public async Task<CommandResult> ExecuteAsync(CancelProposal c,CancellationToken ct)
    {
        var state=await Load(c.SessionId,c.ExpectedVersion,ct); var proposal=state.Proposals.SingleOrDefault(x=>x.ProposalId==c.ProposalId)??throw new DomainException("Proposal does not exist.");
        var cancelled=state.CancelProposal(c.ProposalId,c.ExpectedRevisionNumber,c.Context.For(c.SessionId));state.Apply(cancelled);await store.AppendAsync(c.SessionId,c.ExpectedVersion,[cancelled],state,ct);
        return InteractionResult(state,c.ProposalId,nameof(ProposalCancelled),proposal,[nameof(ProposalCancelled)]);
    }

    public async Task<CommandResult> ExecuteAsync(ExpireDueProposals c,CancellationToken ct)
    {
        var state=await Load(c.SessionId,c.ExpectedVersion,ct);var appended=new List<IDomainEvent>();ExpireDueProposalEvents(state,c.Context,appended);
        if(appended.Count>0)await store.AppendAsync(c.SessionId,c.ExpectedVersion,appended,state,ct);
        return new(state.Id,state.StateVersion,appended.Count==0?"NoDueProposals":nameof(ProposalExpired),AffectedTeamIds:appended.OfType<ProposalExpired>().SelectMany(x=>new[]{x.SenderTeamId,x.ReceiverTeamId}).Distinct().ToList(),EmittedEventTypes:appended.Select(x=>x.GetType().Name).ToList());
    }

    public async Task<CommandResult> ExecuteAsync(ExecuteAgreement c,CancellationToken ct)
    {
        RequireStoryServices();var state=await Load(c.SessionId,c.ExpectedVersion,ct);var package=await LoadLockedPackage(state,ct);var agreement=state.Agreements.SingleOrDefault(x=>x.AgreementId==c.AgreementId)??throw new DomainException("Agreement does not exist.");
        var interaction=package.InteractionDefinitions.Single(x=>x.Id==agreement.InteractionTypeId);if(interaction.ExecutionMode!=InteractionExecutionMode.ManualExecution)throw new DomainException("Agreement is not authored for manual execution.");
        if(c.Context.TeamId is { } team&&!agreement.PartyTeamIds.Contains(team))throw new DomainException("Only an Agreement party may execute it.");
        var appended=new List<IDomainEvent>();ExecuteAgreementEvents(state,package,c.AgreementId,c.Context,appended);await store.AppendAsync(c.SessionId,c.ExpectedVersion,appended,state,ct);
        return new(state.Id,state.StateVersion,nameof(AgreementExecuted),ResourceId:c.AgreementId,AffectedTeamIds:agreement.PartyTeamIds,IsPublic:agreement.Visibility==AgreementVisibility.Public,EmittedEventTypes:appended.Select(x=>x.GetType().Name).ToList());
    }

    public async Task<CommandResult> ExecuteAsync(FailAgreement c,CancellationToken ct)
    {
        var state=await Load(c.SessionId,c.ExpectedVersion,ct);var agreement=state.Agreements.SingleOrDefault(x=>x.AgreementId==c.AgreementId)??throw new DomainException("Agreement does not exist.");
        var failed=state.FailAgreement(c.AgreementId,c.ReasonCode,c.Context.For(c.SessionId));state.Apply(failed);await store.AppendAsync(c.SessionId,c.ExpectedVersion,[failed],state,ct);
        return new(state.Id,state.StateVersion,nameof(AgreementFailed),ResourceId:c.AgreementId,AffectedTeamIds:agreement.PartyTeamIds,IsPublic:agreement.Visibility==AgreementVisibility.Public,EmittedEventTypes:[nameof(AgreementFailed)]);
    }

    private void ExpireDueProposalEvents(StorySession state,CommandContext context,List<IDomainEvent> appended)
    {
        foreach(var proposal in state.Proposals.Where(x=>x.Status is ProposalStatus.Pending or ProposalStatus.Countered&&x.ValidUntilCheckpointId==state.CurrentCheckpointId).OrderBy(x=>x.ProposalId))
            Add(state,appended,state.ExpireProposal(proposal.ProposalId,context.For(state.Id)));
    }

    private void ExecuteDueAgreementEvents(StorySession state,StoryPackage package,CommandContext context,List<IDomainEvent> appended)
    {
        foreach(var agreement in state.Agreements.Where(x=>x.Status==AgreementStatus.Active).OrderBy(x=>x.AgreementId))
        {
            var interaction=package.InteractionDefinitions.Single(x=>x.Id==agreement.InteractionTypeId);
            if(interaction.ExecutionMode==InteractionExecutionMode.ExecuteAtCheckpointResolution) ExecuteAgreementEvents(state,package,agreement.AgreementId,context,appended);
        }
    }

    private void ExecuteAgreementEvents(StorySession state,StoryPackage package,Guid agreementId,CommandContext context,List<IDomainEvent> appended)
    {
        var agreement=state.Agreements.Single(x=>x.AgreementId==agreementId);var proposal=state.Proposals.Single(x=>x.ProposalId==agreement.ProposalId);var revision=state.ProposalRevisions.Single(x=>x.ProposalId==proposal.ProposalId&&x.RevisionNumber==agreement.AcceptedRevisionNumber);
        ApplyAgreementEffects(state,package,revision.OfferedEffects,proposal.ReceiverTeamId,context,agreement,appended);
        ApplyAgreementEffects(state,package,revision.RequestedEffects,proposal.SenderTeamId,context,agreement,appended);
        Add(state,appended,state.ExecuteAgreement(agreementId,context.For(state.Id) with { CausationId=proposal.ProposalId }));
    }

    private void ApplyAgreementEffects(StorySession state,StoryPackage package,IReadOnlyList<string> effectIds,Guid currentTeamId,CommandContext context,Agreement agreement,List<IDomainEvent> appended)
    {
        var entityId=state.Teams.Single(x=>x.Id==currentTeamId).ControlledEntityId;var pseudo=new StoryletAssignment(agreement.AgreementId,"$agreement",state.CurrentCheckpointId!,StoryletScope.TeamPrivate,currentTeamId,entityId,false,state.StateVersion,StoryletAssignmentStatus.Responded);
        foreach(var effectId in effectIds)
        {
            var effect=package.Effects.Single(x=>x.Id==effectId);var metadata=context.For(state.Id) with { TeamId=currentTeamId,CheckpointId=state.CurrentCheckpointId,CausationId=agreement.AgreementId };
            foreach(var @event in effects!.CreateEvents(effect,new(state,package,pseudo,null,null,metadata,state.StateVersion+1,currentTeamId,entityId)))Add(state,appended,@event);
        }
    }

    private static bool TeamMatchesAny(Team team,StorySession state,IReadOnlyList<TargetSelectorDefinition> selectors)=>selectors.Any(selector=>selector.Type switch
    {
        TargetSelectorType.AllTeams=>true,
        TargetSelectorType.TeamControllingEntityDefinition=>team.ControlledEntityId is { } id&&state.Entities.Any(x=>x.Id==id&&x.DefinitionId==selector.EntityDefinitionId),
        _=>false
    });

    internal static string ResolveValidity(StoryPackage package,string current,ProposalValidityDefinition validity)
    {
        if(validity.Type==ProposalValidityType.ValidUntilCheckpoint)
        {
            if(string.IsNullOrWhiteSpace(validity.CheckpointId)||!package.Storylets.Any(x=>x.CheckpointId==validity.CheckpointId))throw new DomainException("Proposal validity references an unknown checkpoint.");return validity.CheckpointId;
        }
        if(validity.CheckpointCount is not { } count||count<1)throw new DomainException("ValidForCheckpointCount requires a positive checkpoint count.");
        var checkpoint=current;
        for(var i=1;i<count;i++)
        {
            var next=package.Storylets.Where(x=>x.CheckpointId==checkpoint).Select(x=>x.NextCheckpointId).Where(x=>x is not null).Distinct(StringComparer.Ordinal).ToList();
            if(next.Count!=1)throw new DomainException("Proposal validity cannot resolve a deterministic future checkpoint.");checkpoint=next[0]!;
        }
        return checkpoint;
    }

    private static CommandResult InteractionResult(StorySession state,Guid resourceId,string eventType,Proposal proposal,IReadOnlyList<string> emitted)=>new(state.Id,state.StateVersion,eventType,ResourceId:resourceId,AffectedTeamIds:[proposal.SenderTeamId,proposal.ReceiverTeamId],EmittedEventTypes:emitted);
}
