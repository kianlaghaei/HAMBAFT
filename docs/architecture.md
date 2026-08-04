# Architecture

```text
Versioned Story Package files
        ↓ validation + canonical SHA-256 lock
JWT-bound REST command → SessionRuntime → StorySession aggregate
                                         ↓ meaningful Domain events
                                  one Marten Session stream
                                         ↓
             SessionState / SessionExperience / PublicWorld projections
                                         ↓
          hash-checked public, Team, inbox and Agreement hydration
```

One Session stream is the concurrency and transaction boundary. Proposal races, Agreement execution, autonomous behavior, delayed consequences, Team effects and checkpoint progression append atomically. Aggregate and projection state rebuild entirely from events; localized text remains in the exact locked Package.

The Domain is generic. Package content owns metric keys, Entity definitions, interaction schemas, behavior profiles, difficulty modifiers and consequence definitions. No Package executes C#, JavaScript or generative AI.

`ProposalInboxView` is keyed by the authenticated Team and derived from the replayable `SessionExperienceView`; only the two parties are considered. `PublicWorldView` receives no proposal negotiation and includes only Agreements and consequences authored Public. SignalR contains identifiers and state versions, never terms.

Deterministic selection uses Package hash, Session seed, Difficulty, event-derived state, stable candidate IDs and stable Entity ordering (`DefinitionId`, then `EntityId`). SharedWorld has no reference or database relationship to HAMBAFT.
# Phase 4 additions

`Hambaft.Narrative.Ink` is a focused adapter below Infrastructure and beside the generic Domain/Application layers. Infrastructure loads immutable package files and hydrates narrative projections through the adapter. No Domain type depends on Ink. The API remains a modular monolith with one Marten stream per Session; Entity Endings, World Ending and completion are one atomic append.

Frontend read flow is `event stream -> inline/replayable projections -> optional Ink hydration -> Team/Public/Admin API`. Package content may name market concepts; Domain remains generic. See [Ink integration](ink-integration.md) and [Ending runtime](ending-runtime.md).

## Phase 5 browser boundary

`apps/hambaft-web` is a separate React/Vite client. REST projections remain authoritative and TanStack Query owns their cache. SignalR contains only identifiers/version hints and invalidates affected queries. Zustand stores credentials and ephemeral UI/setup data, never projections or a second game state. ASP.NET serves the production SPA from generated `src/Hambaft.Api/wwwroot`; Development also supports Vite proxying and restricted CORS.

The API exposes a safe package presentation contract: Entities, metrics, localized interaction labels, typed term schemas, Difficulty and behavior-profile setup data. It omits Effects, Conditions, private narrative and Ending selection definitions. Development-only credential provisioning is environment-gated.
