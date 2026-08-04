namespace Hambaft.Domain;

public sealed class StorySession
{
    public Guid Id { get; private set; }
    public string StoryPackageId { get; private set; } = string.Empty;
    public string StoryVersion { get; private set; } = string.Empty;
    public string ContentHash { get; private set; } = string.Empty;
    public int Seed { get; private set; }
    public string DifficultyId { get; private set; } = "standard";
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
    public string? CurrentCheckpointId { get; private set; }
    public bool NarrativeInitialized { get; private set; }
    public int CurrentNarrativeRevision { get; private set; }
    public string? CurrentWorldStoryletId { get; private set; }
    public string? CurrentWorldNarrativeRef { get; private set; }
    public List<StoryletAssignment> StoryletAssignments { get; private set; } = [];
    public List<SubmittedStoryChoice> SubmittedChoices { get; private set; } = [];
    public HashSet<string> PreviouslyAssignedStoryletIds { get; private set; } = [];
    public HashSet<string> PreviouslySubmittedChoiceIds { get; private set; } = [];
    public int CheckpointResolutionNumber { get; private set; }
    public List<Proposal> Proposals { get; private set; } = [];
    public List<ProposalRevision> ProposalRevisions { get; private set; } = [];
    public List<Agreement> Agreements { get; private set; } = [];
    public List<ScheduledConsequence> ScheduledConsequences { get; private set; } = [];
    public List<AuthoredBehaviorSelection> AuthoredBehaviorSelections { get; private set; } = [];
    public List<EndingResult> EndingResults { get; private set; } = [];

    public static StorySession From(IEnumerable<IDomainEvent> history)
    {
        var session = new StorySession();
        foreach (var @event in history) session.Apply(@event);
        return session;
    }

    public static SessionCreated Create(Guid id, string storyPackageId, string storyVersion, string contentHash, int seed, EventMetadata metadata, string difficultyId = "standard")
    {
        if (id == Guid.Empty) throw new DomainException("Session ID is required.");
        Require(storyPackageId, nameof(storyPackageId));
        Require(storyVersion, nameof(storyVersion));
        Require(contentHash, nameof(contentHash));
        if (metadata.SessionId != id) throw new DomainException("Event metadata Session ID must match the Session.");
        return new(id, storyPackageId.Trim(), storyVersion.Trim(), contentHash.Trim(), seed, metadata, DomainKeys.Normalize(difficultyId,nameof(difficultyId)));
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

    public NarrativeInitialized InitializeNarrative(string packageId,string packageVersion,string contentHash,string checkpointId,EventMetadata metadata)
    {
        if(Status!=SessionStatus.Running) throw new DomainException("Narrative can only be initialized for a running Session.");
        if(NarrativeInitialized) throw new DomainException("Narrative is already initialized.");
        if(!string.Equals(StoryPackageId,packageId,StringComparison.Ordinal)||!string.Equals(StoryVersion,packageVersion,StringComparison.Ordinal)||!string.Equals(ContentHash,contentHash,StringComparison.Ordinal))
            throw new DomainException("The Story Package identity or content hash does not match the Session lock.");
        Require(checkpointId,nameof(checkpointId));
        return new(packageId,packageVersion,contentHash,checkpointId,metadata with { CheckpointId=checkpointId });
    }

    public StoryletAssigned AssignStorylet(Guid assignmentId,string storyletId,string checkpointId,StoryletScope scope,Guid? targetTeamId,Guid? targetEntityId,bool requiredResponse,long assignedAtVersion,EventMetadata metadata)
    {
        if(!NarrativeInitialized) throw new DomainException("Narrative must be initialized before assigning Storylets.");
        if(assignmentId==Guid.Empty||StoryletAssignments.Any(x=>x.AssignmentId==assignmentId)) throw new DomainException("Storylet Assignment ID must be unique and non-empty.");
        Require(storyletId,nameof(storyletId)); Require(checkpointId,nameof(checkpointId));
        if(scope==StoryletScope.TeamPrivate&&targetTeamId is null) throw new DomainException("A TeamPrivate Storylet requires a target Team.");
        if(scope==StoryletScope.EntityPrivate&&targetEntityId is null) throw new DomainException("An EntityPrivate Storylet requires a target Entity.");
        if(targetTeamId is { } teamId&&!Teams.Any(x=>x.Id==teamId)) throw new DomainException("Storylet target Team does not exist.");
        if(targetEntityId is { } entityId&&!Entities.Any(x=>x.Id==entityId)) throw new DomainException("Storylet target Entity does not exist.");
        return new(assignmentId,storyletId,checkpointId,scope,targetTeamId,targetEntityId,requiredResponse,assignedAtVersion,metadata with { CheckpointId=checkpointId,StoryletAssignmentId=assignmentId,TeamId=targetTeamId??metadata.TeamId });
    }

    public StoryChoiceSubmitted SubmitChoice(Guid assignmentId,Guid teamId,string choiceId,IReadOnlyCollection<string> validChoiceIds,long submittedAtVersion,EventMetadata metadata)
    {
        if(!NarrativeInitialized) throw new DomainException("Narrative is not initialized.");
        var assignment=StoryletAssignments.SingleOrDefault(x=>x.AssignmentId==assignmentId)??throw new DomainException("Storylet Assignment does not exist.");
        if(assignment.TargetTeamId!=teamId) throw new DomainException("The Storylet Assignment belongs to another Team.");
        if(assignment.Status is StoryletAssignmentStatus.Resolved or StoryletAssignmentStatus.Superseded) throw new DomainException("The Storylet Assignment is no longer active.");
        if(SubmittedChoices.Any(x=>x.AssignmentId==assignmentId)) throw new DomainException("A Choice has already been submitted for this Assignment.");
        if(!validChoiceIds.Contains(choiceId,StringComparer.Ordinal)) throw new DomainException("The Choice is not available for this Storylet Assignment.");
        return new(assignmentId,teamId,choiceId,metadata.OccurredAtUtc,submittedAtVersion,metadata with { TeamId=teamId,CheckpointId=assignment.CheckpointId,StoryletAssignmentId=assignmentId,ChoiceSubmissionId=metadata.CommandId });
    }

    public NarrativeCheckpointResolved ResolveCheckpoint(string nextCheckpointId,IReadOnlyList<Guid> resolvedAssignmentIds,EventMetadata metadata)
    {
        if(!NarrativeInitialized||CurrentCheckpointId is null) throw new DomainException("Narrative is not initialized.");
        var missing=StoryletAssignments.Where(x=>x.CheckpointId==CurrentCheckpointId&&x.RequiredResponse&&x.Status!=StoryletAssignmentStatus.Responded).ToList();
        if(missing.Count>0) throw new DomainException("Required Storylet responses are still missing.");
        Require(nextCheckpointId,nameof(nextCheckpointId));
        return new(CurrentCheckpointId,nextCheckpointId,resolvedAssignmentIds,metadata with { CheckpointId=CurrentCheckpointId });
    }

    public ProposalSent SendProposal(Guid proposalId,string interactionTypeId,Guid receiverTeamId,System.Text.Json.JsonElement terms,IReadOnlyList<string> offeredEffects,IReadOnlyList<string> requestedEffects,string validUntilCheckpointId,long createdAtVersion,EventMetadata metadata)
    {
        EnsureInteractionRuntime();
        var senderTeamId=metadata.TeamId??throw new DomainException("Authenticated Team identity is required.");
        if(proposalId==Guid.Empty||Proposals.Any(x=>x.ProposalId==proposalId)) throw new DomainException("Proposal ID must be unique and non-empty.");
        if(senderTeamId==receiverTeamId) throw new DomainException("A Proposal requires distinct sender and receiver Teams.");
        if(!Teams.Any(x=>x.Id==senderTeamId)||!Teams.Any(x=>x.Id==receiverTeamId)) throw new DomainException("Proposal Teams must belong to the same Session.");
        if(Proposals.Any(x=>x.InteractionTypeId==interactionTypeId&&x.SenderTeamId==senderTeamId&&x.ReceiverTeamId==receiverTeamId&&x.Status is ProposalStatus.Pending or ProposalStatus.Countered)) throw new DomainException("A conflicting outstanding Proposal already exists.");
        Require(interactionTypeId,nameof(interactionTypeId)); Require(validUntilCheckpointId,nameof(validUntilCheckpointId));
        return new(proposalId,interactionTypeId,senderTeamId,receiverTeamId,1,terms.Clone(),offeredEffects.ToList(),requestedEffects.ToList(),CurrentCheckpointId!,validUntilCheckpointId,createdAtVersion,metadata with { CheckpointId=CurrentCheckpointId });
    }

    public ProposalCountered CounterProposal(Guid proposalId,int expectedRevision,System.Text.Json.JsonElement terms,long createdAtVersion,EventMetadata metadata)
    {
        var proposal=ActiveProposal(proposalId); var teamId=metadata.TeamId??throw new DomainException("Authenticated Team identity is required.");
        if(proposal.CurrentRevisionNumber!=expectedRevision) throw new DomainException("Counterproposal revision is stale.");
        if(ExpectedResponder(proposal)!=teamId) throw new DomainException("Only the receiving party for the current revision may counter.");
        var current=CurrentRevision(proposal);
        return new(proposalId,proposal.InteractionTypeId,proposal.SenderTeamId,proposal.ReceiverTeamId,expectedRevision+1,teamId,terms.Clone(),current.OfferedEffects.ToList(),current.RequestedEffects.ToList(),createdAtVersion,metadata with { CheckpointId=CurrentCheckpointId });
    }

    public ProposalAccepted AcceptProposal(Guid proposalId,int expectedRevision,Guid agreementId,EventMetadata metadata)
    {
        var proposal=ActiveProposal(proposalId); var teamId=metadata.TeamId??throw new DomainException("Authenticated Team identity is required.");
        if(proposal.CurrentRevisionNumber!=expectedRevision) throw new DomainException("Only the current Proposal revision may be accepted.");
        if(ExpectedResponder(proposal)!=teamId) throw new DomainException("The current Team is not allowed to accept this revision.");
        if(agreementId==Guid.Empty||Agreements.Any(x=>x.AgreementId==agreementId)) throw new DomainException("Agreement ID must be unique and non-empty.");
        return new(proposal.ProposalId,proposal.InteractionTypeId,proposal.SenderTeamId,proposal.ReceiverTeamId,proposal.CurrentRevisionNumber,agreementId,metadata with { CheckpointId=CurrentCheckpointId });
    }

    public ProposalRejected RejectProposal(Guid proposalId,int expectedRevision,EventMetadata metadata)
    {
        var proposal=ActiveProposal(proposalId); var teamId=metadata.TeamId??throw new DomainException("Authenticated Team identity is required.");
        if(proposal.CurrentRevisionNumber!=expectedRevision) throw new DomainException("Only the current Proposal revision may be rejected.");
        if(ExpectedResponder(proposal)!=teamId) throw new DomainException("The current Team is not allowed to reject this revision.");
        return new(proposal.ProposalId,proposal.InteractionTypeId,proposal.SenderTeamId,proposal.ReceiverTeamId,proposal.CurrentRevisionNumber,metadata with { CheckpointId=CurrentCheckpointId });
    }

    public ProposalCancelled CancelProposal(Guid proposalId,int expectedRevision,EventMetadata metadata)
    {
        var proposal=ActiveProposal(proposalId); var teamId=metadata.TeamId??throw new DomainException("Authenticated Team identity is required.");
        if(proposal.CurrentRevisionNumber!=expectedRevision) throw new DomainException("Only the current Proposal revision may be cancelled.");
        if(CurrentRevision(proposal).CreatedByTeamId!=teamId) throw new DomainException("Only the Team awaiting a response may cancel its outstanding offer.");
        return new(proposal.ProposalId,proposal.InteractionTypeId,proposal.SenderTeamId,proposal.ReceiverTeamId,proposal.CurrentRevisionNumber,metadata with { CheckpointId=CurrentCheckpointId });
    }

    public ProposalExpired ExpireProposal(Guid proposalId,EventMetadata metadata)
    {
        var proposal=ActiveProposal(proposalId);
        return new(proposal.ProposalId,proposal.InteractionTypeId,proposal.SenderTeamId,proposal.ReceiverTeamId,proposal.CurrentRevisionNumber,metadata with { CheckpointId=CurrentCheckpointId });
    }

    public AgreementActivated ActivateAgreement(Guid proposalId,Guid agreementId,AgreementVisibility visibility,EventMetadata metadata)
    {
        var proposal=Proposals.SingleOrDefault(x=>x.ProposalId==proposalId)??throw new DomainException("Proposal does not exist.");
        if(proposal.Status!=ProposalStatus.Accepted||proposal.AgreementId!=agreementId) throw new DomainException("Proposal has not been accepted for this Agreement.");
        var revision=CurrentRevision(proposal);
        return new(agreementId,proposalId,revision.RevisionNumber,proposal.InteractionTypeId,[proposal.SenderTeamId,proposal.ReceiverTeamId],revision.TermsPayload.Clone(),CurrentCheckpointId!,visibility,metadata with { CheckpointId=CurrentCheckpointId });
    }

    public AgreementExecuted ExecuteAgreement(Guid agreementId,EventMetadata metadata)
    {
        var agreement=Agreements.SingleOrDefault(x=>x.AgreementId==agreementId)??throw new DomainException("Agreement does not exist.");
        if(agreement.Status!=AgreementStatus.Active) throw new DomainException("Only an active Agreement may execute.");
        var proposal=Proposals.Single(x=>x.ProposalId==agreement.ProposalId);
        return new(agreementId,agreement.ProposalId,agreement.InteractionTypeId,agreement.PartyTeamIds,proposal.CurrentRevisionNumber,CurrentCheckpointId!,metadata with { CheckpointId=CurrentCheckpointId });
    }

    public AgreementFailed FailAgreement(Guid agreementId,string reasonCode,EventMetadata metadata)
    {
        var agreement=Agreements.SingleOrDefault(x=>x.AgreementId==agreementId)??throw new DomainException("Agreement does not exist.");
        if(agreement.Status!=AgreementStatus.Active) throw new DomainException("Only an active Agreement may fail.");
        var proposal=Proposals.Single(x=>x.ProposalId==agreement.ProposalId);
        return new(agreementId,agreement.ProposalId,agreement.InteractionTypeId,agreement.PartyTeamIds,proposal.CurrentRevisionNumber,CurrentCheckpointId!,DomainKeys.Normalize(reasonCode,nameof(reasonCode)),metadata with { CheckpointId=CurrentCheckpointId });
    }

    public EntityEndingResolved ResolveEntityEnding(EndingResult result,EventMetadata metadata)
    {
        if(Status!=SessionStatus.Running) throw new DomainException("Endings require a running Session.");
        if(result.Scope!=EndingScope.Entity||result.SessionId!=Id||EndingResults.Any(x=>x.Scope==EndingScope.Entity&&x.ScopeId==result.ScopeId)) throw new DomainException("Entity Ending is invalid or already resolved.");
        var entity=Entities.SingleOrDefault(x=>x.Id==result.ScopeId)??throw new DomainException("Ending Entity does not belong to this Session.");
        if(entity.ControllerType!=ControllerType.HumanTeam) throw new DomainException("Only controlled Entity Endings are required by default.");
        return new(result,metadata with { TeamId=entity.ControlledByTeamId,CheckpointId=CurrentCheckpointId });
    }

    public WorldEndingResolved ResolveWorldEnding(EndingResult result,EventMetadata metadata)
    {
        if(Status!=SessionStatus.Running||result.Scope!=EndingScope.World||result.SessionId!=Id||result.ScopeId!=Id) throw new DomainException("World Ending is invalid for this Session.");
        if(EndingResults.Any(x=>x.Scope==EndingScope.World)) throw new DomainException("World Ending is already resolved.");
        if(Entities.Where(x=>x.ControllerType==ControllerType.HumanTeam).Any(x=>EndingResults.All(r=>r.Scope!=EndingScope.Entity||r.ScopeId!=x.Id))) throw new DomainException("All controlled Entity Endings must resolve before the World Ending.");
        return new(result,metadata with { CheckpointId=CurrentCheckpointId });
    }

    public SessionCompleted Complete(EventMetadata metadata)
    {
        if(Status!=SessionStatus.Running||EndingResults.Count(x=>x.Scope==EndingScope.World)!=1) throw new DomainException("Session completion requires one World Ending.");
        return new(metadata with { CheckpointId=CurrentCheckpointId });
    }

    public void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case SessionCreated e: Id=e.Id; StoryPackageId=e.StoryPackageId; StoryVersion=e.StoryVersion; ContentHash=e.ContentHash; Seed=e.Seed; DifficultyId=e.DifficultyId; Status=SessionStatus.Created; CreatedAtUtc=e.Metadata.OccurredAtUtc; break;
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
            case NarrativeInitialized e: NarrativeInitialized=true; CurrentCheckpointId=e.CheckpointId; break;
            case StoryletAssigned e:
                StoryletAssignments.Add(new(e.AssignmentId,e.StoryletId,e.CheckpointId,e.Scope,e.TargetTeamId,e.TargetEntityId,e.RequiredResponse,e.AssignedAtVersion,StoryletAssignmentStatus.Assigned));
                PreviouslyAssignedStoryletIds.Add(e.StoryletId);
                break;
            case StoryChoiceSubmitted e:
                SubmittedChoices.Add(new(e.AssignmentId,e.TeamId,e.ChoiceId,e.SubmittedAtUtc,e.SubmittedAtStreamVersion,e.Metadata.CommandId));
                PreviouslySubmittedChoiceIds.Add(e.ChoiceId);
                ReplaceAssignment(e.AssignmentId,x=>x with { Status=StoryletAssignmentStatus.Responded });
                break;
            case NarrativeCheckpointResolved e:
                CurrentCheckpointId=e.NextCheckpointId;
                CheckpointResolutionNumber++;
                foreach(var assignmentId in e.ResolvedAssignmentIds) ReplaceAssignment(assignmentId,x=>x with { Status=StoryletAssignmentStatus.Resolved });
                break;
            case MetricChanged e: UpsertMetric(e.Scope,e.ScopeId,e.MetricKey,e.NewValue); break;
            case MetricSet e: UpsertMetric(e.Scope,e.ScopeId,e.MetricKey,e.NewValue); break;
            case StoryMemoryAdded e: Memories.Add(new(e.Scope,e.ScopeId,e.Key,e.OptionalJsonValue,e.Visibility,e.Metadata.OccurredAtUtc)); break;
            case StoryMemoryRemoved e: Memories.RemoveAll(x=>x.Scope==e.Scope&&x.ScopeId==e.ScopeId&&x.Key==e.Key); break;
            case RelationshipChanged e: UpsertRelationship(e.SourceEntityId,e.TargetEntityId,e.RelationshipKey,e.NewValue); break;
            case WorldNarrativePublished e: CurrentWorldStoryletId=e.StoryletId; CurrentWorldNarrativeRef=e.NarrativeRef; CurrentNarrativeRevision=e.Revision; break;
            case ProposalSent e:
                Proposals.Add(new(e.ProposalId,Id,e.InteractionTypeId,e.SenderTeamId,e.ReceiverTeamId,e.CurrentRevisionNumber,ProposalStatus.Pending,e.CreatedAtCheckpointId,e.ValidUntilCheckpointId,e.CreatedAtStreamVersion));
                ProposalRevisions.Add(new(e.ProposalId,e.CurrentRevisionNumber,e.SenderTeamId,e.TermsPayload.Clone(),e.OfferedEffects,e.RequestedEffects,e.Metadata.OccurredAtUtc,e.CreatedAtStreamVersion));
                break;
            case ProposalCountered e:
                ReplaceProposal(e.ProposalId,x=>x with { CurrentRevisionNumber=e.CurrentRevisionNumber,Status=ProposalStatus.Countered });
                ProposalRevisions.Add(new(e.ProposalId,e.CurrentRevisionNumber,e.CreatedByTeamId,e.TermsPayload.Clone(),e.OfferedEffects,e.RequestedEffects,e.Metadata.OccurredAtUtc,e.CreatedAtStreamVersion));
                break;
            case ProposalAccepted e: ReplaceProposal(e.ProposalId,x=>x with { Status=ProposalStatus.Accepted,AcceptedRevisionNumber=e.CurrentRevisionNumber,AgreementId=e.AgreementId }); break;
            case ProposalRejected e: ReplaceProposal(e.ProposalId,x=>x with { Status=ProposalStatus.Rejected }); break;
            case ProposalCancelled e: ReplaceProposal(e.ProposalId,x=>x with { Status=ProposalStatus.Cancelled }); break;
            case ProposalExpired e: ReplaceProposal(e.ProposalId,x=>x with { Status=ProposalStatus.Expired }); break;
            case AgreementActivated e: Agreements.Add(new(e.AgreementId,e.ProposalId,e.AcceptedRevisionNumber,e.InteractionTypeId,e.PartyTeamIds,e.TermsPayload.Clone(),AgreementStatus.Active,e.ActivatedAtCheckpointId,null,null,e.Visibility)); break;
            case AgreementExecuted e:
                ReplaceAgreement(e.AgreementId,x=>x with { Status=AgreementStatus.Executed,ExecutedAtCheckpointId=e.ExecutedAtCheckpointId });
                ReplaceProposal(e.ProposalId,x=>x with { Status=ProposalStatus.Executed });
                break;
            case AgreementFailed e:
                ReplaceAgreement(e.AgreementId,x=>x with { Status=AgreementStatus.Failed,FailedAtCheckpointId=e.FailedAtCheckpointId });
                ReplaceProposal(e.ProposalId,x=>x with { Status=ProposalStatus.Failed });
                break;
            case AuthoredBehaviorActionSelected e: AuthoredBehaviorSelections.Add(new(e.EntityId,e.BehaviorProfileId,e.RuleId,e.ActionId,e.DifficultyId,e.Metadata.CheckpointId??CurrentCheckpointId??string.Empty,e.ResolutionNumber)); break;
            case ConsequenceScheduled e: ScheduledConsequences.Add(new(e.ScheduledConsequenceId,e.DefinitionId,e.SourceEventId,e.SourceTeamId,e.SourceEntityId,e.ScheduledAtCheckpointId,e.DueCheckpointId,e.TriggerType,ScheduledConsequenceStatus.Pending,e.Visibility)); break;
            case ConsequenceTriggered e: ReplaceConsequence(e.ScheduledConsequenceId,x=>x with { Status=ScheduledConsequenceStatus.Triggered }); break;
            case ConsequenceCancelled e: ReplaceConsequence(e.ScheduledConsequenceId,x=>x with { Status=ScheduledConsequenceStatus.Cancelled }); break;
            case ConsequenceFailed e: ReplaceConsequence(e.ScheduledConsequenceId,x=>x with { Status=ScheduledConsequenceStatus.Failed }); break;
            case EntityEndingResolved e: EndingResults.Add(e.Result); break;
            case WorldEndingResolved e: EndingResults.Add(e.Result); break;
            case SessionCompleted e: Status=SessionStatus.Completed; CompletedAtUtc=e.Metadata.OccurredAtUtc; break;
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
    private void ReplaceAssignment(Guid id,Func<StoryletAssignment,StoryletAssignment> change) { var i=StoryletAssignments.FindIndex(x=>x.AssignmentId==id); if(i<0) throw new DomainException("Storylet Assignment does not exist."); StoryletAssignments[i]=change(StoryletAssignments[i]); }
    private void ReplaceProposal(Guid id,Func<Proposal,Proposal> change) { var i=Proposals.FindIndex(x=>x.ProposalId==id); if(i<0) throw new DomainException("Proposal does not exist."); Proposals[i]=change(Proposals[i]); }
    private void ReplaceAgreement(Guid id,Func<Agreement,Agreement> change) { var i=Agreements.FindIndex(x=>x.AgreementId==id); if(i<0) throw new DomainException("Agreement does not exist."); Agreements[i]=change(Agreements[i]); }
    private void ReplaceConsequence(Guid id,Func<ScheduledConsequence,ScheduledConsequence> change) { var i=ScheduledConsequences.FindIndex(x=>x.ScheduledConsequenceId==id); if(i<0) throw new DomainException("Scheduled Consequence does not exist."); ScheduledConsequences[i]=change(ScheduledConsequences[i]); }
    private void UpsertMetric(MetricScope scope,Guid id,string key,decimal value) { var i=Metrics.FindIndex(x=>x.Scope==scope&&x.ScopeId==id&&x.MetricKey==key); if(i<0) Metrics.Add(new(scope,id,key,value)); else Metrics[i]=Metrics[i] with { NumericValue=value }; }
    private void UpsertRelationship(Guid source,Guid target,string key,decimal value) { var i=Relationships.FindIndex(x=>x.SourceEntityId==source&&x.TargetEntityId==target&&x.RelationshipKey==key); if(i<0) Relationships.Add(new(source,target,key,value)); else Relationships[i]=Relationships[i] with { NumericValue=value }; }
    private static void Require(string value,string name) { if(string.IsNullOrWhiteSpace(value)) throw new DomainException($"{name} is required."); }
    private DomainException InvalidTransition(string action) => new($"Cannot {action} a Session in {Status} status.");
    private void EnsureInteractionRuntime() { if(Status!=SessionStatus.Running||!NarrativeInitialized||CurrentCheckpointId is null) throw new DomainException("Interactions require a running initialized narrative."); }
    private Proposal ActiveProposal(Guid proposalId)
    {
        EnsureInteractionRuntime(); var proposal=Proposals.SingleOrDefault(x=>x.ProposalId==proposalId)??throw new DomainException("Proposal does not exist.");
        if(proposal.Status is not (ProposalStatus.Pending or ProposalStatus.Countered)) throw new DomainException("Proposal is no longer active."); return proposal;
    }
    private ProposalRevision CurrentRevision(Proposal proposal)=>ProposalRevisions.Single(x=>x.ProposalId==proposal.ProposalId&&x.RevisionNumber==proposal.CurrentRevisionNumber);
    private Guid ExpectedResponder(Proposal proposal)
    {
        var creator=CurrentRevision(proposal).CreatedByTeamId; return creator==proposal.SenderTeamId?proposal.ReceiverTeamId:proposal.SenderTeamId;
    }
}
