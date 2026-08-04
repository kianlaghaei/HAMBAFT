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
