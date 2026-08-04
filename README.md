# HAMBAFT

HAMBAFT is a generic multiplayer interactive-story backend. Phase 1 contains the independent .NET 10 solution, generic Session/Team/World Entity state, event-sourced commands, Marten/PostgreSQL persistence, replayable inline projections, optimistic concurrency, the REST setup flow, pairing-code exchange for scoped Team JWTs, health checks, OpenAPI, and an authorized typed SignalR hub.

Not implemented: storylets, choices, decisions, proposals, agreements, conditions, effects, delayed consequences, behavior resolution, endings, Ink, Hezar Cheragh content, runtime generative AI, or a frontend.

## Stack

.NET 10, ASP.NET Core, PostgreSQL, Marten, SignalR, JWT bearer authentication, OpenAPI, xUnit and FluentAssertions.

## Run locally

PostgreSQL must already be installed and running. Create `hambaft_dev` and `hambaft_test`; never point HAMBAFT at a SharedWorld database. Configure secrets as shown in `docs/local-development.md`.

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Hambaft.Api
```

Then execute `src/Hambaft.Api/Hambaft.Api.http`, including pairing and SignalR negotiation. No Docker fallback exists.
