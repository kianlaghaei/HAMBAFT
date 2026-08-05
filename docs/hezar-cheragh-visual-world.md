# Hezar Cheragh visual world

## Scope

The visual world is a Hezar Cheragh-specific presentation feature in `apps/hambaft-web`. It is not a generic HAMBAFT scene engine and it never decides game state. It renders the current REST projections and is refreshed after SignalR invalidation hints.

The 0.2.0 package remains immutable. Phase 6B adds `hezar-cheragh/0.3.0`, retaining the same authoritative story/effect runtime while adding hash-covered semantic presentation files. Runtime code still loads only compiled Ink JSON; Ink remains presentation-only.

## Visible world

The market frame contains four stable business locations, the central courtyard, Haj Sadegh's closed office, and the entrance/cargo route. Stable placement is keyed by entity definition ID; session entity IDs remain the identity used for live relationships and movement.

Uncontrolled entities receive an ambient activity mark. This means only that the public entity projection identifies them as authored/system controlled. It does not expose the selected behavior rule. Avan, shortage, rumours, pressure, gathering, and endings appear from authored presentation tags or the current public checkpoint. These visual cues do not run Conditions or Effects.

## Proposal and relationship language

Team views can render the proposal data already present in that team's experience projection:

| Authoritative status | Presentation |
|---|---|
| Pending / Sent | sealed letter travels from sender |
| Countered | letter returns with a second seal |
| Accepted | agreement connection becomes available after the REST projection includes it |
| Rejected | broken/red seal |
| Expired | fading inactive letter |
| Executed | agreement line pulses |
| Failed | agreement line weakens and breaks |

Relationship lines are created only from Backend-authored `RelationshipPresentationState` already authorized for a Team. Raw relationship values never enter React. Public relationship events do not populate the Public projection; Public Display can show only public Agreements. Relationship lines appear for an active Agreement or while a related location is selected.

## Rendering modes

- High loads PixiJS lazily. The canvas is decorative; accessible scene meaning remains in the surrounding React controls and live labels.
- Reduced uses the SVG market without continuous motion. A system `prefers-reduced-motion` preference automatically downgrades High to Reduced.
- Fallback uses SVG/CSS only. Pixi initialization failures select this mode automatically and show a non-blocking status message.

The game controls are never gated on animation completion. Refresh reconstructs time, location conditions and atmosphere from the latest semantic projection. The guided introduction and inspected-location memory are Zustand/session-local presentation state. SignalR reconnect invalidates Team and Public World projections and restores the current authored scene instead of replaying morning.

## Phase 6B observable systems

Pressure changes crowd/queue/messenger/shutter tags; PublicTrust changes approach, notice and body-orientation tags; Autonomy changes independent/Avan/route markers; Transparency changes ledgers, documents and rumour indicators; Resilience changes shared supply, paths and mutual-aid tags. The renderer consumes these distinct authored visual tags and never compares raw values.

Thirteen authored locations provide a compact identity, current condition, recent change, visible characters and authorized actions. Hotspot state is subtle, inspected state stays local, and an equivalent keyboard location list remains available in Pixi, reduced-motion and fallback modes.

## Audio

Audio is off by default and starts only from the user's scene-control click. The Web Audio implementation generates low-volume tones in code; no recorded or copyrighted assets are shipped. The supported sound vocabulary is ambient market, distant carts, paper and seal, crowd tension, rain/water, warehouse warning, and ending ambience. Leaving the scene stops and closes the audio context.

## Gameplay renderers

`rendererRegistry.ts`, `ChoiceList`, and `TermsEditor` provide the package-driven renderer vocabulary: single choice, multiple choice, ranked choice, numeric allocation, numeric range, short declaration, compound decision, target selection, document/evidence selection, and proposal term forms. The 0.2.0 flow uses the existing authoritative single-choice contract and package-defined numeric/boolean/short-text/compound proposal fields. The frontend does not invent an unsupported response payload or execute effects; any additional renderer schema must first be present in the package projection and validated by the Backend.

## Visual checks

Use the Team route at 1440×900 and 820×1180, Public Display at 1600×1000, then repeat Team with `prefers-reduced-motion: reduce` and the scene selector set to Simple Map. Check that the narrative and action areas remain operable, there is no horizontal overflow, four businesses remain visible, and a failed/disabled canvas leaves the SVG map in place.
