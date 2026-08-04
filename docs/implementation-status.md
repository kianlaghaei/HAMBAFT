# Implementation status

| Area | Status | Evidence / limitation |
| --- | --- | --- |
| Phase 1 Session foundation | Complete | Existing aggregate, Marten, REST, pairing, JWT, health and SignalR tests remain green |
| Story Package catalog | Complete | Exact ID/version loading, safe listing, validation errors and canonical SHA-256 tests |
| Condition Engine | Complete for Phase 2 | All 18 declared condition types, explicit target resolution and diagnostic trace tests |
| Effect Engine | Complete for Phase 2 | Seven typed effects, clamp policy and meaningful event tests |
| Storylet selection | Complete for Phase 2 | Priority, stable weighted choice, repeat, condition, target, seed and replay tests |
| Narrative commands | Complete for Phase 2 | Initialize, Team claim-bound choice and atomic checkpoint resolution |
| Privacy projections | Complete for Phase 2 | Team isolation, public exclusion, localized hydration and package mismatch detection |
| PostgreSQL/Marten | Complete for Phase 2 | Persisted assignments/choices/effects, concurrency, atomic append and projection rebuild |
| Sample cargo-delay | Complete | Two HumanTeam Entities reach stable `outcome` checkpoint deterministically |
| Ink and full authored platform | Deferred | Not part of Phase 2 |

## Operational notes

- Package source is the filesystem, never the database.
- A Session cannot initialize if the installed content hash differs from its lock.
- Resolution order is Storylet priority descending, Assignment ID, Team ID, then authored Choice effect order.
- Metric changes clamp to the package-declared range.
- Projection text hydration checks the package hash.
- Admin/PublicDisplay tokens are deployment-provisioned; Team tokens come from pairing.
