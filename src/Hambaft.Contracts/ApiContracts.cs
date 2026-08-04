namespace Hambaft.Contracts;

using System.Text.Json;

public sealed record CreateSessionRequest(string StoryPackageId,string StoryVersion,string ContentHash,int Seed,long ExpectedVersion=0,string DifficultyId="standard");
public sealed record AddTeamRequest(string DisplayName,long ExpectedVersion);
public sealed record CreateEntityRequest(string DefinitionId,string DisplayName,string ControllerType,string? BehaviorProfileId,long ExpectedVersion);
public sealed record AssignEntityRequest(Guid EntityId,Guid TeamId,long ExpectedVersion);
public sealed record TransitionRequest(long ExpectedVersion);
public sealed record MetricInput(string Scope,Guid ScopeId,string MetricKey,decimal NumericValue);
public sealed record MemoryInput(string Scope,Guid ScopeId,string Key,string? OptionalJsonValue,string Visibility);
public sealed record RelationshipInput(Guid SourceEntityId,Guid TargetEntityId,string RelationshipKey,decimal NumericValue);
public sealed record BootstrapRequest(long ExpectedVersion,IReadOnlyList<MetricInput> Metrics,IReadOnlyList<MemoryInput> Memories,IReadOnlyList<RelationshipInput> Relationships);
public sealed record PairingRequest(Guid TeamId,string PairingCode);
public sealed record TokenResponse(string AccessToken,string TokenType,DateTimeOffset ExpiresAtUtc);
public sealed record CommandResponse(Guid SessionId,long StateVersion,string EventType,Guid? ResourceId=null,string? PairingCode=null);
public sealed record StateChangedNotification(Guid SessionId,long StateVersion,string EventType,string Scope);
public sealed record InitializeNarrativeRequest(long ExpectedStateVersion,Guid CommandId);
public sealed record SubmitStoryChoiceRequest(Guid AssignmentId,string ChoiceId,long ExpectedStateVersion,Guid CommandId);
public sealed record ResolveNarrativeRequest(long ExpectedStateVersion,Guid CommandId);
public sealed record NarrativeNotification(Guid SessionId,Guid? TeamId,long StateVersion,string CheckpointId,string EventType);
public sealed record SendProposalRequest(string InteractionTypeId,Guid ReceiverTeamId,JsonElement TermsPayload,string? ValidityType,int? ValidForCheckpointCount,string? ValidUntilCheckpointId,long ExpectedStateVersion,Guid CommandId);
public sealed record CounterProposalRequest(int ExpectedRevisionNumber,JsonElement TermsPayload,long ExpectedStateVersion,Guid CommandId);
public sealed record ProposalDecisionRequest(int ExpectedRevisionNumber,long ExpectedStateVersion,Guid CommandId);
public sealed record InteractionNotification(Guid SessionId,Guid ProposalId,Guid? AgreementId,long StateVersion,string EventType);

public sealed record SessionResponse(
    Guid Id,
    string StoryPackageId,
    string StoryVersion,
    string ContentHash,
    int Seed,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    IReadOnlyList<TeamResponse> Teams,
    IReadOnlyList<EntityResponse> Entities,
    long StateVersion);

public sealed record TeamResponse(Guid Id,Guid SessionId,string DisplayName,Guid? ControlledEntityId,DateTimeOffset JoinedAtUtc);
public sealed record EntityResponse(Guid Id,Guid SessionId,string DefinitionId,string DisplayName,string ControllerType,Guid? ControlledByTeamId,string? BehaviorProfileId,string Status);
