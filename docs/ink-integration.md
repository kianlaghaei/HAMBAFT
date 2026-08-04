# Ink integration

## Authority boundary

Ink is an authored presentation runtime. It renders paragraphs, dialogue, localized choice labels, ending text and presentation tags. `StorySession` and Application services remain the only authority for metrics, memories, relationships, choices, Effects, Proposals, Agreements, authored behavior, consequences and Endings. The adapter has no Marten, database or event-store dependency and never returns a raw Ink runtime object.

Production runtime loads only `narrative/compiled/*.ink.json`. Source `.ink` files are retained for review and both source and compiled files participate in the canonical package hash. Development compilation is explicit:

```powershell
dotnet run --project src/Hambaft.Ink.Compiler -- <source.ink> <compiled.ink.json>
```

No package is compiled on API startup.

## Package contract

`ink.json` declares every compiled file/entry point and its `WorldPublic`, `TeamPrivate`, `EntityPrivate` or `AdminOnly` scope. It also declares allowed scalar variables with type, required status and `Public`/`Private` visibility. Assignment of an unknown variable, wrong scalar type, missing required input, or private input during public rendering is rejected before Ink runs.

Approved tags are `scene`, `sound`, `music`, `camera`, `animation`, `portrait`, `mood`, `speaker`, `effect`, `narrative_ref` and `visibility`. An `effect` value is only a stable package Effect ID; it is validated and returned as metadata, never executed by Ink. Command-like tag names/values are rejected. A package can opt into unknown non-dangerous presentation tags, but Hezar Cheragh does not.

## Adapter

`Hambaft.Narrative.Ink` exposes `IInkStoryLoader`, `IInkNarrativeRenderer`, `IInkStateSerializer` and `IInkPackageValidator`. Loaded runtime handles hide Qyl Ink types. The renderer selects a declared entry point, assigns approved inputs, continues text, reads choices and tags, and can follow an explicitly supplied choice index for text flow.

Validated immutable compiled stories are cached by package ID, version, content hash and compiled path. Mutable Session/Ink runtime instances are not shared.

## State design

The vertical slice uses stateless narrative references: authoritative events store stable Storylet/Choice/Ending references, and text is reconstructed from the locked package plus current view inputs. Serialized Ink state is supported and tested for future genuinely stateful passages, but Phase 4 does not persist `InkNarrativeStateSaved`. A serialized envelope is bound to package ID, version, hash, narrative ref, scope, target and checkpoint; any mismatch refuses restore. It is never a source of game state.

## Privacy and determinism

Public rendering receives only variables declared `Public`. Team/Entity private references are hydrated only into that Team's projection. Logs contain references and IDs rather than private rendered paragraphs at normal information level. Identical compiled content, inputs, entry point and choice selection produce identical paragraphs, tags and choices.
