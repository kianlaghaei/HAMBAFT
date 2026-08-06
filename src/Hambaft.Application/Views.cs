using Hambaft.Domain;
using System.Text.Json;

namespace Hambaft.Application;

public sealed record ChoicePresentationView(string Renderer,string ShortTitle,string Action,string ImmediateImplication,string? KnownCost,string? KnownRisk,bool UnknownConsequence,string? RelatedLocationId,string? RelatedCharacterId,string? RelatedEntityDefinitionId,string? EvidenceKind);
public sealed record NarrativeChoiceView(string Id,string Label,string? ShortOutcome,ChoicePresentationView? Presentation=null);
public sealed record NarrativeContentView(string StoryletId,string NarrativeRef,string Title,IReadOnlyList<string> Paragraphs,IReadOnlyDictionary<string,string> PresentationTags);
public sealed record TeamStoryletView(Guid AssignmentId,string StoryletId,string CheckpointId,string Title,IReadOnlyList<string> Paragraphs,IReadOnlyDictionary<string,string> PresentationTags,IReadOnlyList<NarrativeChoiceView> Choices,bool RequiredResponse,bool Submitted,string? SubmittedChoiceId);
public sealed record EndingPresentationView(Guid EndingResultId,EndingScope Scope,Guid ScopeId,string EndingDefinitionId,string Title,IReadOnlyList<string> Paragraphs,IReadOnlyDictionary<string,string> PresentationTags,IReadOnlyList<EndingEvidence> Evidence,string ContentHash,long ResolvedAtStreamVersion);
public sealed record SessionMetadataView(Guid SessionId,SessionStatus Status,string StoryPackageId,string StoryVersion,string ContentHash,string DifficultyId,string? CurrentCheckpointId);
public sealed record EndingEligibilityDiagnostic(EndingScope Scope,Guid ScopeId,string EndingDefinitionId,bool Eligible,int Priority);
public sealed record SemanticStateView(string Key,string BandId,string Label,string Description,IReadOnlyList<string> VisualTags,string Trend="steady");
public sealed record LocationPresentationStateView(string Id,string DisplayName,string ShortIdentity,string WhyItMatters,string CurrentCondition,string WhoIsHere,string RecentChange,IReadOnlyList<string> AvailableActions,IReadOnlyList<string> PresentationTags,decimal X,decimal Y,Guid? EntityId,string? EntityDefinitionId,string? BusinessIdentity = null);
public sealed record CharacterPresentationStateView(string Id,string DisplayName,string LocationId,string WhatIsKnown,string LastSeen,string Attitude,string RecentStatement,string? PossibleInteraction,IReadOnlyList<string> PresentationTags);
public sealed record AmbientPresentationStateView(string Id,string Description,string LocationId);
public sealed record ReactionPresentationStateView(string OutcomeLine,IReadOnlyList<string> LocationIds,IReadOnlyList<string> VisualTags);
public sealed record BusinessPresentationStateView(Guid EntityId,string EntityDefinitionId,string DisplayName,IReadOnlyList<string> Pulse,IReadOnlyList<string> VisualTags,string? BusinessIdentity = null);
public sealed record RelationshipPresentationStateView(Guid SourceEntityId,Guid TargetEntityId,string RelationshipKey,string BandId,string Label,string Description,IReadOnlyList<string> VisualTags);
public sealed record InteractionTermSchemaPresentationView(string Type,string? Name,bool Required,decimal? Minimum,decimal? Maximum,int? MaximumLength,IReadOnlyList<InteractionTermSchemaPresentationView> Fields);
public sealed record InvestigationEvidencePresentationView(string Id,string Title,string SourceLabel,string Description,string WhyItMatters,string Certainty,IReadOnlyList<string> Unlocks);
public sealed record InvestigationLocationPresentationView(string Id,string Title,string Identity,string Narrative,IReadOnlyList<InvestigationEvidencePresentationView> Evidence);
public sealed record TeamBusinessActivityPresentationView(string EntityDefinitionId,string Title,string Summary,string Implication,IReadOnlyList<string> UnlockedActions);
public sealed record ContextualPactPresentationView(string Id,string InteractionTypeId,string TargetEntityDefinitionId,Guid TargetTeamId,string TargetDisplayName,string Title,string Description,string Unlock,string Risk,InteractionTermSchemaPresentationView TermsSchema,string? Message = null);
public sealed record DecisionChoicePresentationView(string ChoiceId,string SelectedAction,string AcceptedRisk,string Position,string PactUsed,string Summary);
public sealed record FinalDecisionPresentationView(string Heading,string Summary,IReadOnlyList<DecisionChoicePresentationView> Choices,string? SelectedChoiceId,DecisionChoicePresentationView? SelectedChoice);
public sealed record TeamMarketReactionPresentationView(string ChoiceId,string OutcomeLine,IReadOnlyList<string> LocationIds,IReadOnlyList<string> VisualTags);
public sealed record TeamSceneAuthoredActionView(string Id,string Label,string Kind,string? TargetId,bool Available);
public sealed record TeamSceneHotspotView(string Id,string Label,decimal X,decimal Y,string StorySheetId,bool Shared,bool Investigated);
public sealed record TeamSceneStorySheetView(string Id,string Title,string Subtitle,IReadOnlyList<string> Narrative,IReadOnlyList<InvestigationEvidencePresentationView> Evidence,IReadOnlyList<TeamSceneAuthoredActionView> Actions);
public sealed record TeamSceneNextPresentationView(string Title,string Subtitle,IReadOnlyList<string> Narrative,IReadOnlyList<TeamSceneAuthoredActionView> Actions);
public sealed record TeamScenePresentationView(string SceneId,string Title,IReadOnlyList<string> OpeningNarrative,string Objective,int RequiredInvestigationCount,IReadOnlyList<InvestigationLocationPresentationView> InvestigationLocations,TeamBusinessActivityPresentationView? BusinessActivity,IReadOnlyList<ContextualPactPresentationView> ContextualPacts,FinalDecisionPresentationView? FinalDecisionPresentation,IReadOnlyList<TeamMarketReactionPresentationView> MarketReactions,IReadOnlyList<string> InvestigatedLocationIds,IReadOnlyList<string> RevealedEvidenceIds,string Subtitle,string? BackgroundAssetId,string? BackgroundAssetUrl,IReadOnlyList<TeamSceneHotspotView> Hotspots,IReadOnlyList<TeamSceneStorySheetView> StorySheets,IReadOnlyList<TeamSceneAuthoredActionView> AuthoredActions,TeamSceneNextPresentationView? NextScenePresentation);
public sealed record WorldPresentationStateView(string SceneId,string TimeOfDay,string TimeLabel,string AtmosphereLabel,string PublicEvent,string CourtyardActivity,string Soundscape,bool AvanVisible,IReadOnlyList<string> VisualTags,IReadOnlyList<string> Pulse,IReadOnlyList<SemanticStateView> SemanticMetrics,IReadOnlyList<LocationPresentationStateView> Locations,IReadOnlyList<BusinessPresentationStateView> Businesses,IReadOnlyList<CharacterPresentationStateView> Characters,IReadOnlyList<AmbientPresentationStateView> AmbientEvents,IReadOnlyList<ReactionPresentationStateView> Reactions);
public sealed record AdminSessionView(Guid Id,IReadOnlyList<Guid> TeamsAwaitingResponse,IReadOnlyList<string> AssignedStoryletIds,string? Checkpoint,IReadOnlyList<Proposal> OpenProposals,IReadOnlyList<Agreement> ActiveAgreements,IReadOnlyList<ScheduledConsequence> PendingConsequences,IReadOnlyList<AuthoredBehaviorSelection> UncontrolledBehaviorSelections,IReadOnlyList<EndingEligibilityDiagnostic> EndingEligibilityDiagnostics,string PackageVersion,string ContentHash,long StreamVersion,IReadOnlyList<Metric>? TechnicalMetrics=null,WorldPresentationStateView? MarketPreview=null);

public sealed record SessionStateView(
    Guid Id,string StoryPackageId,string StoryVersion,string ContentHash,int Seed,SessionStatus Status,
    DateTimeOffset CreatedAtUtc,DateTimeOffset? StartedAtUtc,DateTimeOffset? CompletedAtUtc,
    IReadOnlyList<Team> Teams,IReadOnlyList<WorldEntity> Entities,long StateVersion,
    string? CurrentCheckpointId,bool NarrativeInitialized,IReadOnlyList<StoryletAssignment> StoryletAssignments,IReadOnlyList<SubmittedStoryChoice> SubmittedChoices,
    string DifficultyId="standard",int CheckpointResolutionNumber=0,IReadOnlyList<Proposal>? Proposals=null,IReadOnlyList<ProposalRevision>? ProposalRevisions=null,IReadOnlyList<Agreement>? Agreements=null,IReadOnlyList<ScheduledConsequence>? ScheduledConsequences=null,IReadOnlyList<AuthoredBehaviorSelection>? AuthoredBehaviorSelections=null,IReadOnlyList<EndingResult>? EndingResults=null)
{
    public IReadOnlyList<Guid> TeamsAwaitingResponse=>StoryletAssignments.Where(x=>x.RequiredResponse&&x.Status==StoryletAssignmentStatus.Assigned&&x.TargetTeamId is not null).Select(x=>x.TargetTeamId!.Value).Distinct().OrderBy(x=>x).ToList();
    public IReadOnlyList<string> AssignedStoryletIds=>StoryletAssignments.Select(x=>x.StoryletId).ToList();
    public IReadOnlyList<string> SubmittedChoiceIds=>SubmittedChoices.Select(x=>x.ChoiceId).ToList();
}

public sealed record ProposalRevisionView(int RevisionNumber,Guid CreatedByTeamId,JsonElement TermsPayload,DateTimeOffset CreatedAtUtc);
public sealed record ProposalItemView(Guid ProposalId,string InteractionTypeId,Guid SenderTeamId,Guid ReceiverTeamId,int CurrentRevisionNumber,JsonElement TermsPayload,ProposalStatus Status,string DeadlineCheckpoint,IReadOnlyList<string> AllowedActions,long StateVersion,IReadOnlyList<ProposalRevisionView>? Revisions=null,string? ContextualMessage=null);
public sealed record ProposalInboxView(Guid Id,Guid SessionId,IReadOnlyList<ProposalItemView> Incoming,IReadOnlyList<ProposalItemView> Outgoing,long StateVersion);
public sealed record AgreementView(Guid AgreementId,string InteractionTypeId,IReadOnlyList<Guid> Parties,JsonElement TermsPayload,AgreementStatus Status,string ActivationCheckpoint,string? ExecutionCheckpoint,AgreementVisibility Visibility);
public sealed record ScheduledConsequenceView(Guid ScheduledConsequenceId,string DefinitionId,string? DueCheckpointId,ConsequenceTriggerType TriggerType,ScheduledConsequenceStatus Status,ConsequenceVisibility Visibility);
public sealed record AvailableInteractionView(string InteractionTypeId,IReadOnlyList<Guid> AllowedTargetTeamIds);

public sealed record TeamExperienceView(
    Guid Id,Guid SessionId,Team Team,WorldEntity? ControlledEntity,IReadOnlyList<Metric> VisibleMetrics,
    IReadOnlyList<StoryMemory> VisibleMemories,IReadOnlyList<Relationship> VisibleRelationships,long StateVersion,
    string? CurrentCheckpointId,IReadOnlyList<TeamStoryletView> PrivateStorylets,
    IReadOnlyList<ProposalItemView>? Inbox=null,IReadOnlyList<ProposalItemView>? Outbox=null,IReadOnlyList<AgreementView>? Agreements=null,IReadOnlyList<AvailableInteractionView>? AvailableInteractionTypes=null,IReadOnlyList<ScheduledConsequenceView>? PendingConsequences=null,EndingPresentationView? EntityEnding=null,EndingPresentationView? WorldEnding=null,SessionMetadataView? SessionMetadata=null,WorldPresentationStateView? WorldPresentation=null,BusinessPresentationStateView? BusinessPresentation=null,IReadOnlyList<RelationshipPresentationStateView>? RelationshipPresentation=null,TeamScenePresentationView? TeamScenePresentation=null)
{
    public IReadOnlyList<AgreementView> ActiveAgreements => (Agreements??[]).Where(x=>x.Status==AgreementStatus.Active).ToList();
}

public sealed record PublicWorldView(
    Guid Id,SessionStatus Status,IReadOnlyList<WorldEntity> Entities,IReadOnlyList<Metric> WorldMetrics,
    IReadOnlyList<StoryMemory> PublicMemories,IReadOnlyList<Relationship> PublicRelationships,long StateVersion,
    string StoryPackageId,string StoryVersion,string ContentHash,string? CurrentCheckpointId,string? CurrentWorldStoryletId,string? CurrentWorldNarrativeRef,NarrativeContentView? Narrative,
    IReadOnlyList<AgreementView>? PublicAgreements=null,IReadOnlyList<ScheduledConsequenceView>? PublicConsequences=null,EndingPresentationView? WorldEnding=null,IReadOnlyList<EndingPresentationView>? PublicEntityEndingSummaries=null,string DifficultyId="standard",WorldPresentationStateView? WorldPresentation=null);

public sealed record EntityStateView(Guid Id,Guid SessionId,ControllerType ControllerType,Guid? ControlledByTeamId,EntityStatus Status,IReadOnlyList<Metric> Metrics,IReadOnlyList<StoryMemory> VisibleMemories,long StateVersion);

public sealed record SessionExperienceView(
    Guid Id,SessionStatus Status,IReadOnlyList<Team> Teams,IReadOnlyList<WorldEntity> Entities,IReadOnlyList<Metric> Metrics,
    IReadOnlyList<StoryMemory> Memories,IReadOnlyList<Relationship> Relationships,long StateVersion,
    string StoryPackageId,string StoryVersion,string ContentHash,string? CurrentCheckpointId,
    IReadOnlyList<StoryletAssignment> StoryletAssignments,IReadOnlyList<SubmittedStoryChoice> SubmittedChoices,
    string DifficultyId="standard",int CheckpointResolutionNumber=0,IReadOnlyList<Proposal>? Proposals=null,IReadOnlyList<ProposalRevision>? ProposalRevisions=null,IReadOnlyList<Agreement>? Agreements=null,IReadOnlyList<ScheduledConsequence>? ScheduledConsequences=null,IReadOnlyList<AuthoredBehaviorSelection>? AuthoredBehaviorSelections=null,IReadOnlyList<EndingResult>? EndingResults=null);

public sealed record EndingEvidenceView(Guid Id,IReadOnlyList<EndingResult> EntityEndings,EndingResult? WorldEnding,string PackageId,string PackageVersion,string ContentHash,long StateVersion);

public static class ViewProjector
{
    public static SessionStateView Session(StorySession s)=>new(s.Id,s.StoryPackageId,s.StoryVersion,s.ContentHash,s.Seed,s.Status,s.CreatedAtUtc,s.StartedAtUtc,s.CompletedAtUtc,s.Teams.ToList(),s.Entities.ToList(),s.StateVersion,s.CurrentCheckpointId,s.NarrativeInitialized,s.StoryletAssignments.ToList(),s.SubmittedChoices.ToList(),s.DifficultyId,s.CheckpointResolutionNumber,s.Proposals.ToList(),s.ProposalRevisions.ToList(),s.Agreements.ToList(),s.ScheduledConsequences.ToList(),s.AuthoredBehaviorSelections.ToList(),s.EndingResults.ToList());
    public static PublicWorldView Public(StorySession s)=>new(s.Id,s.Status,s.Entities.ToList(),s.Metrics.Where(x=>x.Scope==MetricScope.World).ToList(),s.Memories.Where(x=>x.Visibility==MemoryVisibility.Public).ToList(),[],s.StateVersion,s.StoryPackageId,s.StoryVersion,s.ContentHash,s.CurrentCheckpointId,s.CurrentWorldStoryletId,s.CurrentWorldNarrativeRef,null,s.Agreements.Where(x=>x.Visibility==AgreementVisibility.Public).Select(ToAgreementView).ToList(),s.ScheduledConsequences.Where(x=>x.Visibility==ConsequenceVisibility.Public).Select(ToConsequenceView).ToList(),DifficultyId:s.DifficultyId);
    public static TeamExperienceView Team(StorySession s,Team team,StoryPackage? package=null,string? locale=null)
    {
        var entity=s.Entities.SingleOrDefault(x=>x.Id==team.ControlledEntityId);
        var metrics=s.Metrics.Where(x=>x.Scope==MetricScope.World||x.Scope==MetricScope.Team&&x.ScopeId==team.Id||entity is not null&&x.Scope==MetricScope.Entity&&x.ScopeId==entity.Id).ToList();
        var memories=s.Memories.Where(x=>x.Visibility==MemoryVisibility.Public||x.Visibility==MemoryVisibility.TeamPrivate&&x.Scope==MemoryScope.Team&&x.ScopeId==team.Id||entity is not null&&x.Visibility==MemoryVisibility.EntityPrivate&&x.Scope==MemoryScope.Entity&&x.ScopeId==entity.Id).ToList();
        var relationships=entity is null?[]:s.Relationships.Where(x=>x.SourceEntityId==entity.Id||x.TargetEntityId==entity.Id).ToList();
        var phase3=Phase3(team,s.Proposals,s.ProposalRevisions,s.Agreements,s.ScheduledConsequences,s.StateVersion,package,s.Teams,s.Entities);
        var semantic=package is null||package.PresentationDefinition.MetricBands.Count==0?null:SemanticPresentation.Project(package,s.Id,s.CurrentCheckpointId,s.Entities,metrics,relationships,true);
        return new(team.Id,s.Id,team,entity,semantic is null?metrics:[],memories,semantic is null?relationships:[],s.StateVersion,s.CurrentCheckpointId,package is null?[]:PrivateStorylets(s.StoryletAssignments,s.SubmittedChoices,team,entity,package,locale),phase3.Incoming,phase3.Outgoing,phase3.Agreements,phase3.Interactions,phase3.Consequences,SessionMetadata:new(s.Id,s.Status,s.StoryPackageId,s.StoryVersion,s.ContentHash,s.DifficultyId,s.CurrentCheckpointId),WorldPresentation:semantic?.World,BusinessPresentation:semantic?.Business,RelationshipPresentation:semantic?.Relationships,TeamScenePresentation:package is null?null:TeamScene(package,s.CurrentCheckpointId,s.StoryletAssignments,s.SubmittedChoices,team,entity,phase3.Interactions,s.Entities,memories,locale));
    }
    public static EntityStateView Entity(StorySession s,WorldEntity entity)=>new(entity.Id,s.Id,entity.ControllerType,entity.ControlledByTeamId,entity.Status,s.Metrics.Where(x=>x.Scope==MetricScope.Entity&&x.ScopeId==entity.Id).ToList(),s.Memories.Where(x=>x.Visibility==MemoryVisibility.Public||x.Visibility==MemoryVisibility.EntityPrivate&&x.Scope==MemoryScope.Entity&&x.ScopeId==entity.Id).ToList(),s.StateVersion);
    public static TeamExperienceView Team(SessionExperienceView s,Team team,StoryPackage? package=null,string? locale=null)
    {
        var entity=s.Entities.SingleOrDefault(x=>x.Id==team.ControlledEntityId);
        var metrics=s.Metrics.Where(x=>x.Scope==MetricScope.World||x.Scope==MetricScope.Team&&x.ScopeId==team.Id||entity is not null&&x.Scope==MetricScope.Entity&&x.ScopeId==entity.Id).ToList();
        var memories=s.Memories.Where(x=>x.Visibility==MemoryVisibility.Public||x.Visibility==MemoryVisibility.TeamPrivate&&x.Scope==MemoryScope.Team&&x.ScopeId==team.Id||entity is not null&&x.Visibility==MemoryVisibility.EntityPrivate&&x.Scope==MemoryScope.Entity&&x.ScopeId==entity.Id).ToList();
        var relationships=entity is null?[]:s.Relationships.Where(x=>x.SourceEntityId==entity.Id||x.TargetEntityId==entity.Id).ToList();
        var phase3=Phase3(team,s.Proposals??[],s.ProposalRevisions??[],s.Agreements??[],s.ScheduledConsequences??[],s.StateVersion,package,s.Teams,s.Entities);
        var semantic=package is null||package.PresentationDefinition.MetricBands.Count==0?null:SemanticPresentation.Project(package,s.Id,s.CurrentCheckpointId,s.Entities,metrics,relationships,true);
        return new(team.Id,s.Id,team,entity,semantic is null?metrics:[],memories,semantic is null?relationships:[],s.StateVersion,s.CurrentCheckpointId,package is null?[]:PrivateStorylets(s.StoryletAssignments,s.SubmittedChoices,team,entity,package,locale),phase3.Incoming,phase3.Outgoing,phase3.Agreements,phase3.Interactions,phase3.Consequences,SessionMetadata:new(s.Id,s.Status,s.StoryPackageId,s.StoryVersion,s.ContentHash,s.DifficultyId,s.CurrentCheckpointId),WorldPresentation:semantic?.World,BusinessPresentation:semantic?.Business,RelationshipPresentation:semantic?.Relationships,TeamScenePresentation:package is null?null:TeamScene(package,s.CurrentCheckpointId,s.StoryletAssignments,s.SubmittedChoices,team,entity,phase3.Interactions,s.Entities,memories,locale));
    }
    public static EntityStateView Entity(SessionExperienceView s,WorldEntity entity)=>new(entity.Id,s.Id,entity.ControllerType,entity.ControlledByTeamId,entity.Status,s.Metrics.Where(x=>x.Scope==MetricScope.Entity&&x.ScopeId==entity.Id).ToList(),s.Memories.Where(x=>x.Visibility==MemoryVisibility.Public||x.Visibility==MemoryVisibility.EntityPrivate&&x.Scope==MemoryScope.Entity&&x.ScopeId==entity.Id).ToList(),s.StateVersion);
    public static PublicWorldView Hydrate(PublicWorldView view,StoryPackage package,string? locale=null)
    {
        if(package.PresentationDefinition.MetricBands.Count==0)return view with { Narrative=view.CurrentWorldStoryletId is null||view.CurrentWorldNarrativeRef is null?null:Content(package,view.CurrentWorldStoryletId,view.CurrentWorldNarrativeRef,locale) };
        var semantic=SemanticPresentation.Project(package,view.Id,view.CurrentCheckpointId,view.Entities,view.WorldMetrics,[],false);
        return view with { WorldMetrics=[],PublicRelationships=[],Narrative=view.CurrentWorldStoryletId is null||view.CurrentWorldNarrativeRef is null?null:Content(package,view.CurrentWorldStoryletId,view.CurrentWorldNarrativeRef,locale),WorldPresentation=semantic.World };
    }

    private static TeamScenePresentationView? TeamScene(StoryPackage package,string? checkpointId,IReadOnlyList<StoryletAssignment> assignments,IReadOnlyList<SubmittedStoryChoice> submissions,Team team,WorldEntity? entity,IReadOnlyList<AvailableInteractionView> availableInteractions,IReadOnlyList<WorldEntity> entities,IReadOnlyList<StoryMemory> memories,string? locale)
    {
        if(entity is null||checkpointId is null)return null;
        var authored=TeamSceneResolver.Resolve(package,checkpointId,team,entity,submissions,memories);
        if(authored is null)return null;
        var assignment=assignments.SingleOrDefault(x=>x.CheckpointId==checkpointId&&x.TargetTeamId==team.Id&&(x.Status is StoryletAssignmentStatus.Assigned or StoryletAssignmentStatus.Responded));
        var selectedChoiceId=assignment is null?null:submissions.SingleOrDefault(x=>x.AssignmentId==assignment.AssignmentId)?.ChoiceId;
        var pacts=authored.ContextualPacts.Select(pact=>(Pact:pact,Interaction:package.InteractionDefinitions.SingleOrDefault(x=>x.Id==pact.InteractionTypeId),Target:entities.Where(x=>x.DefinitionId==pact.TargetEntityDefinitionId&&x.ControlledByTeamId is not null&&x.ControlledByTeamId!=team.Id).OrderBy(x=>x.Id).FirstOrDefault()))
            .Where(value=>value.Interaction is not null&&value.Target?.ControlledByTeamId is { } targetTeamId&&availableInteractions.Any(available=>available.InteractionTypeId==value.Interaction!.Id&&available.AllowedTargetTeamIds.Contains(targetTeamId)))
            .Select(x=>new ContextualPactPresentationView(x.Pact.Id,x.Pact.InteractionTypeId,x.Pact.TargetEntityDefinitionId,x.Target!.ControlledByTeamId!.Value,x.Target.DisplayName,x.Pact.Title,x.Pact.Description,x.Pact.Unlock,x.Pact.Risk,Terms(x.Interaction!.TermsSchema),x.Pact.Message))
            .ToList();
        var final=authored.FinalDecisionPresentation;
        var decision=final is null?null:new FinalDecisionPresentationView(final.Heading,final.Summary,final.Choices.Select(ToDecision).ToList(),selectedChoiceId,final.Choices.Where(x=>x.ChoiceId==selectedChoiceId).Select(ToDecision).SingleOrDefault());
        var reactions=selectedChoiceId is null?[]:authored.MarketReactions.Where(x=>x.ChoiceId==selectedChoiceId).Select(x=>new TeamMarketReactionPresentationView(x.ChoiceId,x.OutcomeLine,x.LocationIds,x.VisualTags)).ToList();
        var business=authored.BusinessActivity is null?null:new TeamBusinessActivityPresentationView(entity.DefinitionId,authored.BusinessActivity.Title,authored.BusinessActivity.Summary,authored.BusinessActivity.Implication,authored.BusinessActivity.UnlockedActions);
        var progress=memories.Where(x=>x.Scope==MemoryScope.Team&&x.ScopeId==team.Id&&x.Visibility==MemoryVisibility.TeamPrivate&&x.Key.StartsWith("investigation:",StringComparison.Ordinal))
            .Select(InvestigationMemory).Where(x=>x.CheckpointId==checkpointId).ToList();
        var investigated=progress.Select(x=>x.LocationId).Distinct(StringComparer.Ordinal).ToList();
        var sheets=authored.StorySheetDefinitions.Count>0?authored.StorySheetDefinitions:authored.InvestigationLocations.Select(x=>new TeamSceneStorySheetDefinition(x.Id,x.Title,x.Identity,[x.Narrative],x.Evidence,[])).ToList();
        var sheetViews=sheets.Select(sheet=>new TeamSceneStorySheetView(sheet.Id,sheet.Title,sheet.Subtitle,sheet.Narrative,sheet.Evidence.Select(Evidence).ToList(),sheet.Actions.Select(Action).ToList())).ToList();
        var hotspots=authored.HotspotDefinitions.Count>0?authored.HotspotDefinitions.Select(x=>new TeamSceneHotspotView(x.Id,x.Label,x.X,x.Y,x.StorySheetId,x.Shared,investigated.Contains(x.StorySheetId,StringComparer.Ordinal))).ToList():authored.InvestigationLocations.Select(location=>package.PresentationDefinition.Locations.SingleOrDefault(x=>x.Id==location.Id)).Where(x=>x is not null).Select(x=>new TeamSceneHotspotView(x!.Id,x.DisplayName,x.X,x.Y,x.Id,false,investigated.Contains(x.Id,StringComparer.Ordinal))).ToList();
        var actions=authored.ActionDefinitions.Select(Action).ToList();
        var next=authored.NextScenePresentation is null?null:new TeamSceneNextPresentationView(authored.NextScenePresentation.Title,authored.NextScenePresentation.Subtitle,authored.NextScenePresentation.Narrative,authored.NextScenePresentation.Actions.Select(Action).ToList());
        return new(authored.SceneId,authored.Title,authored.OpeningNarrative,authored.Objective,authored.RequiredInvestigationCount,authored.InvestigationLocations.Select(x=>new InvestigationLocationPresentationView(x.Id,x.Title,x.Identity,x.Narrative,x.Evidence.Select(Evidence).ToList())).ToList(),business,pacts,decision,reactions,investigated,progress.SelectMany(x=>x.EvidenceIds).Distinct(StringComparer.Ordinal).ToList(),authored.Subtitle??string.Empty,authored.BackgroundAssetId,authored.BackgroundAssetId is null?null:"/api/story/scene-asset",hotspots,sheetViews,actions,next);

        static DecisionChoicePresentationView ToDecision(DecisionChoicePresentationDefinition choice)=>new(choice.ChoiceId,choice.SelectedAction,choice.AcceptedRisk,choice.Position,choice.PactUsed,choice.Summary);
        static InvestigationEvidencePresentationView Evidence(InvestigationEvidencePresentationDefinition evidence)=>new(evidence.Id,evidence.Title,evidence.SourceLabel,evidence.Description,evidence.WhyItMatters,evidence.Certainty,evidence.Unlocks);
        TeamSceneAuthoredActionView Action(TeamSceneAuthoredActionDefinition action)
        {
            var count=progress.Select(x=>x.LocationId).Distinct(StringComparer.Ordinal).Count();
            var available=(action.MinimumInvestigations is null||count>=action.MinimumInvestigations)&&(action.MaximumInvestigations is null||count<=action.MaximumInvestigations);
            if(action.Kind=="RecordInvestigation"&&action.TargetId is { } target)available=available&&!investigated.Contains(target,StringComparer.Ordinal)&&count<authored.RequiredInvestigationCount;
            if(action.Kind=="SubmitChoice"&&action.TargetId is { } choice)available=available&&assignment is not null&&package.Storylets.Single(x=>x.Id==assignment.StoryletId).Choices.Any(x=>x.Id==choice)&&selectedChoiceId is null;
            if(action.Kind=="OpenPacts")available=available&&pacts.Count>0;
            return new(action.Id,action.Label,action.Kind,action.TargetId,available);
        }
        static InteractionTermSchemaPresentationView Terms(InteractionTermSchemaDefinition schema)=>new(schema.Type.ToString(),schema.Name,schema.Required,schema.Minimum,schema.Maximum,schema.MaximumLength,(schema.Fields??[]).Select(Terms).ToList());
        static (string CheckpointId,string LocationId,IReadOnlyList<string> EvidenceIds) InvestigationMemory(StoryMemory memory)
        {
            using var document=JsonDocument.Parse(memory.OptionalJsonValue??"{}");
            var root=document.RootElement;
            return (root.GetProperty("checkpointId").GetString()??string.Empty,root.GetProperty("locationId").GetString()??string.Empty,root.TryGetProperty("evidenceIds",out var ids)?ids.EnumerateArray().Select(x=>x.GetString()??string.Empty).Where(x=>x.Length>0).ToList():[]);
        }
    }

    private static IReadOnlyList<TeamStoryletView> PrivateStorylets(IReadOnlyList<StoryletAssignment> assignments,IReadOnlyList<SubmittedStoryChoice> submissions,Team team,WorldEntity? entity,StoryPackage package,string? locale)
        =>assignments.Where(a=>(a.Status is StoryletAssignmentStatus.Assigned or StoryletAssignmentStatus.Responded)&&(a.TargetTeamId==team.Id||entity is not null&&a.Scope==StoryletScope.EntityPrivate&&a.TargetEntityId==entity.Id))
            .OrderBy(a=>a.AssignmentId).Select(a=>
            {
                var storylet=package.Storylets.Single(s=>s.Id==a.StoryletId); var narrative=Localized(package,storylet.NarrativeRef,locale); var submitted=submissions.SingleOrDefault(s=>s.AssignmentId==a.AssignmentId);
                return new TeamStoryletView(a.AssignmentId,a.StoryletId,a.CheckpointId,narrative.Title,narrative.Paragraphs,narrative.PresentationTags,storylet.Choices.Select(c=>{var label=Localized(package,c.LabelRef,locale);var authored=package.PresentationDefinition.Choices.SingleOrDefault(x=>x.ChoiceId==c.Id);return new NarrativeChoiceView(c.Id,label.ChoiceLabel??label.Title,label.ShortOutcome,authored is null?null:new(authored.Renderer,authored.ShortTitle,authored.Action,authored.ImmediateImplication,authored.KnownCost,authored.KnownRisk,authored.UnknownConsequence,authored.RelatedLocationId,authored.RelatedCharacterId,authored.RelatedEntityDefinitionId,authored.EvidenceKind));}).ToList(),a.RequiredResponse,submitted is not null,submitted?.ChoiceId);
            }).ToList();
    private static NarrativeContentView Content(StoryPackage package,string storyletId,string narrativeRef,string? locale)
    { var n=Localized(package,narrativeRef,locale); return new(storyletId,narrativeRef,n.Title,n.Paragraphs,n.PresentationTags); }
    private static NarrativeDefinition Localized(StoryPackage package,string key,string? locale)
    {
        var selected=locale is not null&&package.Narrative.ContainsKey(locale)?locale:package.Manifest.DefaultLocale;
        if(package.Narrative.TryGetValue(selected,out var dictionary)&&dictionary.TryGetValue(key,out var value))return value;
        if(package.InkDefinition.References.Any(x=>x.Id==key))return new(string.Empty,[],null,null,new Dictionary<string,string>());
        throw new DomainException($"Narrative reference '{key}' is missing for locale '{selected}'.");
    }

    private static (IReadOnlyList<ProposalItemView> Incoming,IReadOnlyList<ProposalItemView> Outgoing,IReadOnlyList<AgreementView> Agreements,IReadOnlyList<AvailableInteractionView> Interactions,IReadOnlyList<ScheduledConsequenceView> Consequences) Phase3(Team team,IReadOnlyList<Proposal> proposals,IReadOnlyList<ProposalRevision> revisions,IReadOnlyList<Agreement> agreements,IReadOnlyList<ScheduledConsequence> consequences,long version,StoryPackage? package,IReadOnlyList<Team> teams,IReadOnlyList<WorldEntity> entities)
    {
        ProposalItemView Item(Proposal p)
        {
            var revision=revisions.Single(x=>x.ProposalId==p.ProposalId&&x.RevisionNumber==p.CurrentRevisionNumber);var creator=revision.CreatedByTeamId;var responder=creator==p.SenderTeamId?p.ReceiverTeamId:p.SenderTeamId;var actions=new List<string>();
            if(p.Status is ProposalStatus.Pending or ProposalStatus.Countered){if(team.Id==creator)actions.Add("Cancel");if(team.Id==responder)actions.AddRange(["Accept","Counter","Reject"]);}
            var history=revisions.Where(x=>x.ProposalId==p.ProposalId).OrderBy(x=>x.RevisionNumber).Select(x=>new ProposalRevisionView(x.RevisionNumber,x.CreatedByTeamId,x.TermsPayload.Clone(),x.CreatedAtUtc)).ToList();
            var senderEntity=entities.SingleOrDefault(x=>x.ControlledByTeamId==p.SenderTeamId);
            var receiverEntity=entities.SingleOrDefault(x=>x.ControlledByTeamId==p.ReceiverTeamId);
            var contextualMessage=package?.PresentationDefinition.TeamSceneDefinitions.SelectMany(x=>x.ContextualPacts)
                .Where(x=>x.InteractionTypeId==p.InteractionTypeId&&x.Message is not null)
                .FirstOrDefault(x=>x.TargetEntityDefinitionId==senderEntity?.DefinitionId||x.TargetEntityDefinitionId==receiverEntity?.DefinitionId)?.Message;
            return new(p.ProposalId,p.InteractionTypeId,p.SenderTeamId,p.ReceiverTeamId,p.CurrentRevisionNumber,revision.TermsPayload.Clone(),p.Status,p.ValidUntilCheckpointId,actions,version,history,contextualMessage);
        }
        var party=proposals.Where(x=>x.SenderTeamId==team.Id||x.ReceiverTeamId==team.Id).OrderBy(x=>x.ProposalId).ToList();
        var incoming=party.Where(p=>{var r=revisions.Single(x=>x.ProposalId==p.ProposalId&&x.RevisionNumber==p.CurrentRevisionNumber);return (r.CreatedByTeamId==p.SenderTeamId?p.ReceiverTeamId:p.SenderTeamId)==team.Id;}).Select(Item).ToList();
        var outgoing=party.Except(incoming.Select(x=>proposals.Single(p=>p.ProposalId==x.ProposalId))).Select(Item).ToList();
        var visibleAgreements=agreements.Where(x=>x.PartyTeamIds.Contains(team.Id)).OrderBy(x=>x.AgreementId).Select(ToAgreementView).ToList();
        var visibleConsequences=consequences.Where(x=>x.Status==ScheduledConsequenceStatus.Pending&&(x.Visibility==ConsequenceVisibility.Public||x.Visibility==ConsequenceVisibility.PrivateToSourceTeam&&x.SourceTeamId==team.Id)).OrderBy(x=>x.ScheduledConsequenceId).Select(ToConsequenceView).ToList();
        var available=package?.InteractionDefinitions.Where(x=>SelectorMatches(team,x.AllowedSenderSelectors,entities)).OrderBy(x=>x.Id,StringComparer.Ordinal).Select(x=>new AvailableInteractionView(x.Id,teams.Where(t=>t.Id!=team.Id&&SelectorMatches(t,x.AllowedReceiverSelectors,entities)).Select(t=>t.Id).OrderBy(id=>id).ToList())).ToList()??[];
        return(incoming,outgoing,visibleAgreements,available,visibleConsequences);
    }
    private static bool SelectorMatches(Team team,IReadOnlyList<TargetSelectorDefinition> selectors,IReadOnlyList<WorldEntity> entities)=>selectors.Any(s=>s.Type==TargetSelectorType.AllTeams||s.Type==TargetSelectorType.TeamControllingEntityDefinition&&team.ControlledEntityId is { } id&&entities.Any(e=>e.Id==id&&e.DefinitionId==s.EntityDefinitionId));
    internal static AgreementView ToAgreementView(Agreement x)=>new(x.AgreementId,x.InteractionTypeId,x.PartyTeamIds,x.TermsPayload.Clone(),x.Status,x.ActivatedAtCheckpointId,x.ExecutedAtCheckpointId,x.Visibility);
    internal static ScheduledConsequenceView ToConsequenceView(ScheduledConsequence x)=>new(x.ScheduledConsequenceId,x.DefinitionId,x.DueCheckpointId,x.TriggerType,x.Status,x.Visibility);
}
