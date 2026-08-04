# HAMBAFT

HAMBAFT is a generic multiplayer authored-story runtime on .NET 10 with an RTL React playable client. Phase 4 adds compiled Ink narrative rendering, deterministic Entity and World Endings with structured evidence, and the `hezar-cheragh/0.1.0` vertical slice. Phase 5 makes that slice browser-playable for Admin, Team and Public Display modes. HAMBAFT remains authoritative for state, choices, effects, interactions, consequences, endings, authorization, persistence and replay; Ink supplies authored text and presentation metadata only.

The runtime uses one optimistic-concurrency Marten stream per Session. Story content remains immutable repository data locked by exact Package ID, version and SHA-256 hash. Both `sample-cargo-delay/1.0.0` and `1.1.0` are installed simultaneously; an old Session is never hydrated from new content.

## Stack

.NET 10, ASP.NET Core, PostgreSQL, Marten, SignalR, JWT bearer authentication, OpenAPI, React, TypeScript, Vite, TanStack Query, Zustand, Zod, Vitest and Playwright. PostgreSQL is a local operating-system service; Docker is not used.

## Run locally

Configure `hambaft_dev` and `hambaft_test` as described in [local development](docs/local-development.md), then run:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Hambaft.Api
```

The Hezar Cheragh REST walkthrough is in `src/Hambaft.Api/Hambaft.Api.http`. Format and runtime details are in `docs/story-package-format.md`, `docs/story-runtime.md`, `docs/ink-integration.md`, `docs/ending-runtime.md`, and `docs/hezar-cheragh-vertical-slice.md`.

## Browser client

Development uses a second terminal:

```powershell
npm install --prefix apps/hambaft-web
npm run dev --prefix apps/hambaft-web
```

Open `http://localhost:5173`. For a one-URL local demo, run `npm run build --prefix apps/hambaft-web`, start the API, and open `http://localhost:5297`. See [frontend architecture](docs/frontend-architecture.md), [routes](docs/frontend-routes.md), [design system](docs/frontend-design-system.md), and [realtime](docs/frontend-realtime.md).

Deferred: PixiJS/animated market navigation, final generated artwork, the multi-hour Hezar Cheragh story, visual story editing, production deployment, and runtime generative AI.
