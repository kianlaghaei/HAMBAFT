# Implementation status

| Area | Status | Evidence / limitation |
| --- | --- | --- |
| Phase 1 foundation | Complete | Session, Team, Entity, JWT, pairing, health, SignalR and Marten remain green |
| Phase 2 narrative | Complete | Exact Package lock, typed conditions/effects, Storylets, choices and replay |
| Package 1.1.0 | Complete | Four optional Phase 3 files; 1.0.0 unchanged and independently loadable |
| Proposals | Complete | Typed terms, immutable revisions, eligibility, checkpoint expiry and optimistic races |
| Agreements | Complete | Activation, three execution modes, execution/failure events and visibility |
| Behavior Resolver | Complete | Stable Entity/rule ordering, weighted hash selection, fallback, repeat and Difficulty |
| Scheduled consequences | Complete | Four deterministic triggers, schedule/cancel/trigger state and privacy |
| Checkpoint pipeline | Complete | Atomic Phase 3 ordering with meaningful events |
| Projections | Complete | Admin, public and Team experience/inbox rebuild from events |
| API and SignalR | Complete | Claim-bound endpoints and lightweight Team/public notifications |
| PostgreSQL | Complete | 15 real `hambaft_test` tests; persistence, concurrency and projection rebuild |
| Sample 1.1.0 | Complete | Two Teams, Credit Provider, Agreement, expiration, behavior and delayed review |
| Ink/endings/UI/runtime AI | Deferred | Explicitly out of Phase 3 |

Operational invariants: filesystem Packages are never database content; no Session may substitute a different version/hash; no private terms enter public projections or SignalR; no wall-clock timer participates in replay.
# Phase 4 — implemented

- Compiled Ink loading/rendering, strict inputs/tags, validation and guarded state serialization.
- Deterministic Entity and World Ending resolution, structured evidence, atomic completion and replayable projections.
- Team/Public/Admin frontend-ready views and Ending REST/SignalR contracts with privacy boundaries.
- `hezar-cheragh/0.1.0`: Persian Ink, four businesses/checkpoints, five interactions, five behavior profiles, four delayed consequences, 12 Entity Endings and five World Endings.
- Deterministic 2/3/4-Team and Hard scenarios, PostgreSQL persistence/rebuild/concurrency coverage, and local replay performance guard.

Deferred: React, PixiJS, full multi-hour Hezar Cheragh, Visual Story Editor, final content editing tools, production deployment and runtime AI.

# Phase 5 — implemented

| Area | Status | Evidence / limitation |
| --- | --- | --- |
| React foundation | Complete | Vite, strict TypeScript, Router, Query, Zustand-local-only, Motion, SignalR and Zod |
| RTL design | Complete | Persian-first paper/olive/gold system, responsive Team/Admin/Display and reduced motion |
| Pairing/auth | Complete | Code-only pairing, claim-derived Team identity, sessionStorage and explicit unpair |
| Team play | Complete | private Storylets, Choices, metrics, Memories, Consequences and market |
| Negotiation | Complete | typed terms, immutable revision history, Counter/Accept/Reject/Cancel and 409 handling |
| Admin | Complete | two-to-four Team wizard, package helper, one-time codes and runtime actions/diagnostics |
| Public Display | Complete | public narrative, businesses, Agreements/Consequences and World Ending only |
| Realtime | Complete | centralized connection, focused invalidations, offline/online reconnect and refetch |
| Ending UI | Complete | private Entity evidence and public World result without win/loss reduction |
| Frontend tests | Complete | 14 unit/component/contract tests and one full real-browser Playwright flow |
| Local demo | Complete | Vite production output hosted by ASP.NET static files/fallback |

Deferred after Phase 5: PixiJS market world, full animated navigation, final generated artwork, full multi-hour content, Visual Story Editor, production deployment and runtime AI.

# Phase 6B — implemented, verification pending

| Area | Status | Evidence / limitation |
| --- | --- | --- |
| Hezar Cheragh package | Implemented | `0.3.0` is a new immutable directory; `0.2.0` is unchanged |
| Semantic presentation | Implemented | Backend-owned metric, business and relationship bands; Team/Public raw metric arrays are emptied only for semantic packages |
| Living market | Implemented | Six-part authored time vocabulary, five checkpoint-restored scenes, 13 locations, hotspots, characters and bounded deterministic ambience |
| Contextual play | Implemented | Authored choice cards, inspect-only evidence, spatial Proposal targets and accessible list fallback |
| World reaction | Implemented | Backend/package-authored reaction lines, short skippable sequence and reduced-motion text path |
| Public/Admin | Implemented | Public narrative pulse without numbers; Admin numbers only in collapsible technical diagnostics plus semantic preview |
| Frontend verification | Passed | typecheck, lint, 37 Vitest tests and production build |
| Backend verification | Partial | alternate SDK build passed; 124 runnable tests passed, 15 PostgreSQL tests skipped |
| Browser verification | Pending | Required SDK `10.0.302`, local PostgreSQL story flow and browser visual QA must be available |
