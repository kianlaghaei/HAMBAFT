# Implementation status

| Area | Status | Evidence / limitation |
| --- | --- | --- |
| Independent solution and boundaries | Complete | Build and architecture tests |
| Generic session Domain and event replay | Complete | Unit and end-to-end tests |
| Marten session stream and concurrency | Partial | Requires configured local `hambaft_test` for integration verification |
| Inline public/session projections | Partial | Registered and isolation unit-tested; database verification depends on local PostgreSQL |
| Team/Entity projections | Partial | Transactional Marten documents implemented; conversion to independently registered replayable projections remains |
| REST session flow | Partial | Implemented; smoke depends on local PostgreSQL |
| Pairing code generation/hash | Partial | Interfaces and secure implementation; token exchange route deferred |
| JWT authorization | Partial | JWT validation and claim conventions; full admin/pairing lifecycle deferred |
| SignalR | Partial | Typed hub and server-selected groups; authorization lifecycle depends on issued JWTs |
| Story content and Ink | Deferred | Phase 1 exclusion |
| Runtime authored behavior | Deferred | Phase 1 exclusion |

## Known risks

- PostgreSQL 16 service was detected locally, but SCRAM credentials, `hambaft_dev`, `hambaft_test`, and both connection settings must be supplied by the developer.
- Authentication provisioning is intentionally incomplete; private Team projection has no public unauthenticated endpoint.
