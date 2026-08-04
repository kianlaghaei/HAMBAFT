using Hambaft.Domain;

namespace Hambaft.Application;

public sealed record CommandContext(Guid CorrelationId, Guid CausationId, Guid CommandId, Guid? TeamId, DateTimeOffset OccurredAtUtc)
{
    public EventMetadata For(Guid sessionId) => new(CorrelationId,CausationId,CommandId,sessionId,TeamId,OccurredAtUtc);
}

public sealed record CreateSession(Guid SessionId, string StoryPackageId, string StoryVersion, string ContentHash, int Seed, long ExpectedVersion, CommandContext Context, string DifficultyId = "standard");
public sealed record AddTeam(Guid SessionId, Guid TeamId, string DisplayName, long ExpectedVersion, CommandContext Context);
public sealed record CreateWorldEntity(Guid SessionId, Guid EntityId, string DefinitionId, string DisplayName, ControllerType ControllerType, string? BehaviorProfileId, long ExpectedVersion, CommandContext Context);
public sealed record AssignEntityToTeam(Guid SessionId, Guid EntityId, Guid TeamId, long ExpectedVersion, CommandContext Context);
public sealed record SetInitialMetric(Guid SessionId, MetricScope Scope, Guid ScopeId, string MetricKey, decimal NumericValue, long ExpectedVersion, CommandContext Context);
public sealed record AddInitialMemory(Guid SessionId, MemoryScope Scope, Guid ScopeId, string Key, string? OptionalJsonValue, MemoryVisibility Visibility, long ExpectedVersion, CommandContext Context);
public sealed record SetInitialRelationship(Guid SessionId, Guid SourceEntityId, Guid TargetEntityId, string RelationshipKey, decimal NumericValue, long ExpectedVersion, CommandContext Context);
public sealed record StartSession(Guid SessionId, long ExpectedVersion, CommandContext Context);
public sealed record PauseSession(Guid SessionId, long ExpectedVersion, CommandContext Context);
public sealed record ResumeSession(Guid SessionId, long ExpectedVersion, CommandContext Context);
public sealed record CancelSession(Guid SessionId, long ExpectedVersion, CommandContext Context);
public sealed record InitializeNarrative(Guid SessionId,long ExpectedVersion,CommandContext Context);
public sealed record SubmitStoryChoice(Guid SessionId,Guid AssignmentId,string ChoiceId,long ExpectedVersion,CommandContext Context);
public sealed record ResolveNarrativeCheckpoint(Guid SessionId,long ExpectedVersion,CommandContext Context);
public sealed record SendProposal(Guid SessionId,Guid ProposalId,string InteractionTypeId,Guid ReceiverTeamId,System.Text.Json.JsonElement TermsPayload,ProposalValidityDefinition? Validity,long ExpectedVersion,CommandContext Context);
public sealed record CounterProposal(Guid SessionId,Guid ProposalId,int ExpectedRevisionNumber,System.Text.Json.JsonElement TermsPayload,long ExpectedVersion,CommandContext Context);
public sealed record AcceptProposal(Guid SessionId,Guid ProposalId,int ExpectedRevisionNumber,long ExpectedVersion,CommandContext Context);
public sealed record RejectProposal(Guid SessionId,Guid ProposalId,int ExpectedRevisionNumber,long ExpectedVersion,CommandContext Context);
public sealed record CancelProposal(Guid SessionId,Guid ProposalId,int ExpectedRevisionNumber,long ExpectedVersion,CommandContext Context);
public sealed record ExpireDueProposals(Guid SessionId,long ExpectedVersion,CommandContext Context);
public sealed record ExecuteAgreement(Guid SessionId,Guid AgreementId,long ExpectedVersion,CommandContext Context);
public sealed record FailAgreement(Guid SessionId,Guid AgreementId,string ReasonCode,long ExpectedVersion,CommandContext Context);
public sealed record ScheduleConsequence(Guid SessionId,string DefinitionId,Guid SourceEventId,Guid? SourceTeamId,Guid? SourceEntityId,long ExpectedVersion,CommandContext Context);
public sealed record CancelConsequence(Guid SessionId,Guid ScheduledConsequenceId,long ExpectedVersion,CommandContext Context);
public sealed record ResolveDueConsequences(Guid SessionId,long ExpectedVersion,CommandContext Context);
public sealed record ResolveUncontrolledEntityActions(Guid SessionId,long ExpectedVersion,CommandContext Context);
public sealed record CommandResult(Guid SessionId, long StateVersion, string EventType, string? PairingCode = null, Guid? ResourceId = null, IReadOnlyList<Guid>? AffectedTeamIds = null, bool IsPublic = false, IReadOnlyList<string>? EmittedEventTypes = null);

public sealed class SessionNotFoundException(Guid id) : Exception($"Session {id:D} was not found.");
public sealed class ConcurrencyConflictException(Guid id, long expected, long actual) : Exception($"Session {id:D} expected stream version {expected}, but current version is {actual}.")
{
    public long ExpectedVersion { get; }=expected;
    public long ActualVersion { get; }=actual;
}
