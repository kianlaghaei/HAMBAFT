# Implementation status

| Area | Status | Evidence / limitation |
| --- | --- | --- |
| Independent solution and boundaries | Complete | Build and architecture tests |
| Generic session Domain and event replay | Complete | Unit and end-to-end tests |
| Marten session stream and concurrency | Complete | Local `hambaft_test` integration tests verify ordered appends, expected versions and a real stale-writer conflict |
| Inline public/session projections | Complete | Registered Marten projections verified against PostgreSQL and aggregate replay |
| Team/Entity projections | Complete | Registered `SessionExperience` projection is transactionally inline and daemon-rebuild tested; scoped views are materialized without imperative document writes |
| REST session flow | Complete | Automated integration smoke and local `hambaft_dev` smoke cover create through resume plus health endpoints |
| Pairing code generation/hash | Complete | PBKDF2 hash persistence, fixed-time verification, generic failure response and Team token exchange route |
| JWT authorization | Complete | Issued Team tokens contain role/session/team scope and pass issuer, audience, signature and lifetime validation |
| SignalR | Complete | Typed hub requires the `SessionClient` policy, rejects anonymous negotiation and accepts a paired Team JWT |
| Story content and Ink | Deferred | Phase 1 exclusion |
| Runtime authored behavior | Deferred | Phase 1 exclusion |

## Phase 1 verification

- Verified on 2026-08-04 with the local operating-system PostgreSQL service only.
- `hambaft_dev` passed the complete REST/pairing/SignalR/health smoke flow.
- `hambaft_test`, schema `hambaft`, passed the database integration suite, including projection rebuild and optimistic concurrency.
- `dotnet restore`, `dotnet build`, and `dotnet test` are the completion gates.

## Operational notes

- Configure a stable JWT signing key of at least 32 UTF-8 bytes. If omitted, a process-local random key is used and tokens intentionally stop working after restart.
- Team JWTs are issued by pairing. Admin and PublicDisplay credentials are deployment-provisioned; there is no unauthenticated endpoint that mints those roles.
- Team experience views remain internal in Phase 1; no unauthenticated private-state endpoint exists.
