# Story Package format

A Package is immutable repository content at `stories/{id}/{version}`. Required files remain `manifest.json`, `metrics.json`, `entities.json`, `storylets.json`, `effects.json`, and `narrative.json`. Phase 3 adds optional `interactions.json`, `behaviors.json`, `consequences.json`, and `difficulties.json`; absence means an empty Phase 3 catalog and keeps 1.0 content valid.

## Phase 3 definitions

- Interaction: stable ID and narrative refs; sender/receiver Team selectors; a recursively typed `Numeric`, `Boolean`, `ShortText`, or `Compound` term schema; offered/requested effect IDs; checkpoint validity; execution mode; Agreement visibility.
- Validity: `ValidUntilCheckpoint` or positive `ValidForCheckpointCount`. Wall-clock expiry is unsupported.
- Execution: `ImmediateOnAcceptance`, `ManualExecution`, or `ExecuteAtCheckpointResolution`.
- Behavior catalog: profiles contain eligible Entity definitions, prioritized weighted rules, conditions, repeat policy and fallback Action. Actions contain typed effect IDs and optional authored public narrative.
- Difficulty: Package IDs such as Calm, Standard, Hard and Relentless. Explicit modifiers adjust behavior rule weights, typed proposal minimum multipliers, resource thresholds or fallback preference.
- Consequence: trigger, conditions, effect IDs, optional narrative, visibility and repeat policy. Triggers are `AfterCheckpointCount`, `AtCheckpoint`, `WhenAgreementExecuted`, and `WhenMemoryExistsAtResolution`.
- Effects additionally support `ScheduleConsequence` and `CancelConsequence`.

Every reference is validated: effects, narrative, Entities, Actions, behavior profiles, metric scopes, consequence definitions, trigger checkpoints and positive weights/counts. Short text is persisted/displayed only and is never interpreted by AI.

## Canonical hash and compatibility

Every file, including optional files, participates in the canonical SHA-256 hash. Paths are ordinally sorted, JSON object properties are ordinally sorted, arrays retain authored order, and non-JSON line endings normalize to LF. Sessions lock exact ID/version/hash. `sample-cargo-delay` 1.0.0 and 1.1.0 have distinct hashes and remain independently loadable.
# Phase 4 package files

A Phase 4 package may add:

```text
narrative/*.ink
narrative/compiled/*.ink.json
ink.json
endings/entity-endings.json
endings/world-endings.json
```

The canonical SHA-256 content hash covers every package file in stable relative-path order, including both Ink source and compiled output. Runtime loads compiled JSON only. `ink.json` declares allowed scalar variables, visibility, approved tags, compiled paths, entry points, scopes and required variables. All Storylet/Choice/Ending narrative refs, `effect` tags, evidence references, Entity eligibility and aggregate Conditions are validated before a package becomes active. Immutable packages are cached by ID, semantic version and content hash.

Ending definitions use typed Conditions, integer priority/positive weight, structured evidence selectors and presentation tags. Every playable Entity definition needs at least one candidate; each Hezar business has an unconditional fallback. At least one unconditional World Ending is mandatory. Hidden Endings use ordinary difficult Conditions.
