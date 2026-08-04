# Scheduled consequences

`ScheduleConsequence` and `CancelConsequence` are typed Effects. Runtime records stable scheduled ID, definition, source event/Team/Entity, scheduling checkpoint, optional due checkpoint, trigger, status and visibility.

Supported triggers:

- `AfterCheckpointCount`: derive a due checkpoint through deterministic authored checkpoint edges.
- `AtCheckpoint`: match an explicit authored checkpoint.
- `WhenAgreementExecuted`: inspect Agreement execution facts, optionally by interaction type.
- `WhenMemoryExistsAtResolution`: inspect explicit World/current Team/current Entity memory scope.

Pending consequences resolve before autonomous behavior and Team choices. A trigger emits `ConsequenceTriggered` once, then typed effects and optional public narrative. Cancelled or already triggered consequences do not run again. Any checkpoint batch failure is atomic and does not partially append effects.

Visibility is `SystemOnly`, `PrivateToSourceTeam`, or `Public`. Team Experience exposes only own private or public pending consequences; PublicWorld exposes only Public. SignalR follows the same Team/session/display boundary and carries no private payload.

In sample 1.1.0, `promise-original-deadline` schedules `deadline-promise-review` for `outcome`. If pressure remains, Supplier Reliability falls by 4, Supplier→Carrier Trust by 2 and a public authored review appears. Disclosure does not schedule this penalty.
