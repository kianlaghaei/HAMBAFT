# Story runtime

## Conditions

Supported types are `MetricAbove`, `MetricAtLeast`, `MetricBelow`, `MetricAtMost`, `MetricEquals`, `MemoryExists`, `MemoryMissing`, `RelationshipAbove`, `RelationshipBelow`, `EntityControllerIs`, `EntityDefinitionIs`, `TeamControlsEntityDefinition`, `SessionStatusIs`, `CheckpointIs`, `ChoiceWasSubmitted`, `All`, `Any`, and `Not`.

Every state target is explicit: `World`, `CurrentTeam`, `CurrentEntity`, `ExplicitEntityDefinition`, or `RelationshipBetweenCurrentAndTarget`. Evaluation is read-only and may return a trace containing type, resolved target, actual, expected and result. Private traces are not exposed through Team APIs.

## Effects

Supported types are `ChangeMetric`, `SetMetric`, `AddMemory`, `RemoveMemory`, `ChangeRelationship`, `AssignStorylet`, and `PublishWorldNarrative`. Effects produce `MetricChanged`, `MetricSet`, `StoryMemoryAdded`, `StoryMemoryRemoved`, `RelationshipChanged`, `StoryletAssigned`, and `WorldNarrativePublished`. Numeric results clamp to declared min/max. Effect IDs remain trace metadata; fact events remain authoritative.

## Lifecycle

1. `InitializeNarrative` loads exact package ID/version, compares SHA-256 lock, validates running state and Team/Entity requirements, installs package-owned initial metrics/relationships, assigns entry public/private Storylets and publishes the public entry.
2. `SubmitStoryChoice` derives Session/Team from JWT claims, checks ownership, authored choice, duplicate/resolved state and expected stream version, then appends `StoryChoiceSubmitted`.
3. `ResolveNarrativeCheckpoint` refuses missing required responses. It orders submissions by priority descending → Assignment ID → Team ID, executes each choice's effects in authored order, resolves prior assignments, advances the checkpoint, selects next Storylets and publishes the eligible public result in one optimistic Marten transaction.

Selection filters checkpoint, scope, target, conditions and repeat policy; keeps maximum priority; sorts stable Storylet IDs; and applies deterministic weighted selection from package hash, Session seed, stream version and candidate set.

## Privacy and replay

Team experience hydration sees only assignments targeted to that Team or its controlled Entity, plus public/own-private memories and visible relationships. Public view contains only public narrative, World metrics, public memories/entities and relationships. SignalR sends only Session ID, optional Team ID, state version, checkpoint and event type; clients refetch REST.

Aggregate and projection state rebuild solely from Marten events. Localized paragraphs are resolved from stable IDs after verifying the installed package hash. A mismatch raises a conflict instead of silently changing history.

## Sample walkthrough

At `response`, the public entry announces a delayed shared cargo. Supplier privately chooses disclosure or the original promise; Carrier privately chooses emergency capacity or normal operations. Disclosure plus emergency capacity yields `cargo-delay-coordinated-outcome`, World Pressure 48, Supplier Reliability 60, Carrier Capacity 55, private memories and directional Trust changes. The Session reaches stable checkpoint `outcome`; ending resolution is intentionally absent.

Deferred: Ink, proposals, agreements, Behavior Resolver, difficulty, scheduled consequences, entity endings, world endings, React and full Hezar Cheragh content.
