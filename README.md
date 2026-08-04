# HAMBAFT

HAMBAFT is a generic multiplayer interactive-story backend on .NET 10. Phase 2 adds a deterministic, replayable authored-story runtime to the Phase 1 Session/Team/Entity foundation.

The runtime loads versioned repository Story Packages, canonicalizes and hashes all package files, locks each Session to an exact ID/version/hash, evaluates typed conditions, assigns public and private Storylets, records authored Team choices, executes typed effects in stable order, and rebuilds Marten projections entirely from events. The included `sample-cargo-delay` package exercises the complete two-Team flow in Persian with RTL-compatible UTF-8 content.

## Stack

.NET 10, ASP.NET Core, PostgreSQL, Marten, SignalR, JWT bearer authentication, OpenAPI, xUnit and FluentAssertions. PostgreSQL runs as a local operating-system service; Docker is not used.

## Run locally

Configure `hambaft_dev` and `hambaft_test` as described in [local development](docs/local-development.md), then run:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Hambaft.Api
```

Use `src/Hambaft.Api/Hambaft.Api.http` for the real two-Team smoke flow. Story Package details are documented in `docs/story-package-format.md`; runtime lifecycle and privacy boundaries are documented in `docs/story-runtime.md`.

Not implemented: Ink, proposals, agreements, uncontrolled-entity Behavior Resolver, difficulty, scheduled consequences, entity endings, world endings, React, full Hezar Cheragh content, runtime generative AI, or a visual story editor.
