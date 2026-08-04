# Ending runtime

## Definitions and selection

Packages define `endings/entity-endings.json` and `endings/world-endings.json`. Definitions contain narrative refs, typed Conditions, priority, weight, evidence selectors and presentation tags. Entity definitions additionally list eligible Entity definitions and may expose a public summary. A hidden ending is simply a high-priority definition with difficult Conditions.

`IEntityEndingResolver` and `IWorldEndingResolver` apply this stable order:

1. typed Condition eligibility;
2. maximum priority;
3. deterministic weighted tie-break derived from package hash, Session seed, difficulty and scope identity;
4. stable Ending ID ordering.

Each Team-controlled Entity receives exactly one primary Entity Ending. `ResolveUncontrolledEntityEndings` is false in Hezar Cheragh, so uncontrolled businesses receive no private ending. One World Ending is mandatory. Each business and the world have unconditional fallbacks.

Aggregate semantics are deterministic: Entities sort by definition ID then Entity ID; relationship edges sort by source then target; distribution/count tests count values strictly above/below their threshold; network average/minimum operate on all matching relationship-key edges; active and executed Agreements count; expiration counts final Proposal status.

## Atomic completion

`POST /api/sessions/{sessionId}/endings/resolve` is Admin-only and requires `expectedStateVersion` plus `commandId`. It rejects a stale version, unlocked/mismatched package, incomplete slice, active required Storylets, open Proposals, or pending consequences with `409 Conflict`. It creates Entity results first, then World result, then `SessionCompleted`, and appends the complete batch in one optimistic-concurrency Marten transaction. A completed Session cannot be resolved twice.

## Evidence

Every `EndingResult` records scope/ID, definition, exact package lock, resolved stream version, deterministic input fingerprint, narrative reference, presentation tags and UTC audit time. `EndingEvidence` is typed rather than prose and can reference Domain event IDs, Choice/Proposal/Agreement IDs, memory keys, metric snapshots, relationship changes, behavior Action IDs and Consequence IDs. Metric snapshots contain scope, target, key and final value.

`EndingEvidenceView` is an inline replayable projection containing Entity/World results and evidence categories. Evidence allows a report to connect the stable-price promise to the later review, Reliability/Inventory state and an emergency supply Agreement.

## Privacy and publication

`TeamExperienceView` renders only the controlled Entity Ending plus Team-visible evidence and the public World Ending. `PublicWorldView` exposes the World Ending, only public evidence, and Entity summaries only when `publicSummaryNarrativeRef` exists. Admin receives complete structured results/diagnostics. SignalR publishes only IDs/version: a private Entity notification goes to its Team group; World and completion notifications go to Session, Display and Admin groups. Notifications contain no evidence or private paragraphs.
