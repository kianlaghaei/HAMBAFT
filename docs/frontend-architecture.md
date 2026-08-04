# Frontend architecture

Phase 5 adds `apps/hambaft-web`, a strict TypeScript React application. Its dependency direction is:

```text
routes/features -> typed API + query keys -> HAMBAFT REST projections
routes/features -> centralized realtime manager -> query invalidation -> REST refetch
local UI/auth -> Zustand/sessionStorage
```

The frontend never evaluates Conditions, resolves Effects, selects AuthoredBehavior, validates Proposal authority, advances checkpoints, or calculates Endings. Mutation requests include Backend projection versions and command IDs. A successful mutation invalidates its projection; a SignalR message is only a refetch hint.

## Server and local state

TanStack Query owns Team Experience, Public World, Admin, Session and package projections. Query keys are centralized in `src/api/queryKeys.ts`. Zustand stores three isolated credential slots (Team, Admin, PublicDisplay), reduced-motion/UI preferences, and one-time pairing slips generated during setup. It does not contain a game-state mirror.

JWT identity is decoded only to route the credential and read `client_role`, `session_id`, and `team_id`. The Team never selects a Team ID. Authoritative identity remains the validated Backend claims plus Team projection.

## API boundary

`src/api/client.ts` is the only fetch boundary. It adds bearer credentials, parses RFC Problem Details, gives 409 a consistent stale-state message, never retries mutations, and invokes Zod response schemas. Manual schemas are used because the Phase 4 OpenAPI response bodies are mostly unspecified; contract tests demonstrate both successful representative parsing and explicit failure on malformed state.

## Feature layout

- `pairing`: code-only identity bootstrap;
- `team-experience`, `narrative`, `decisions`: Team shell, Storylets and Choices;
- `proposals`, `agreements`: typed negotiation and accepted commitments;
- `consequences`, `endings`: visible history and Backend-authored results;
- `admin`: package-driven setup and runtime operations;
- `public-display`: privacy-safe room projection;
- `realtime`: one connection manager and event/query mapping.
