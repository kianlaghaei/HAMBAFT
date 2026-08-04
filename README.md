# HAMBAFT

HAMBAFT is a generic multiplayer authored-story backend on .NET 10. Phase 3 adds structured Team-to-Team proposals, immutable counter revisions, accepted Agreements, checkpoint expiration, Package-owned difficulty, deterministic behavior for uncontrolled Entities, and replayable scheduled consequences to the Phase 2 narrative runtime.

The runtime uses one optimistic-concurrency Marten stream per Session. Story content remains immutable repository data locked by exact Package ID, version and SHA-256 hash. Both `sample-cargo-delay/1.0.0` and `1.1.0` are installed simultaneously; an old Session is never hydrated from new content.

## Stack

.NET 10, ASP.NET Core, PostgreSQL, Marten, SignalR, JWT bearer authentication, OpenAPI, xUnit and FluentAssertions. PostgreSQL is a local operating-system service; Docker is not used.

## Run locally

Configure `hambaft_dev` and `hambaft_test` as described in [local development](docs/local-development.md), then run:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Hambaft.Api
```

The Phase 3 REST walkthrough is in `src/Hambaft.Api/Hambaft.Api.http`. Format and runtime details are in `docs/story-package-format.md`, `docs/story-runtime.md`, `docs/interactions-runtime.md`, `docs/behavior-runtime.md`, and `docs/scheduled-consequences.md`.

Deferred: Ink, Entity endings, World endings, React, full Hezar Cheragh content, runtime generative AI, and a visual story editor.
