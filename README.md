# HAMBAFT

HAMBAFT is a generic multiplayer interactive-story backend. Phase 1 contains the independent .NET 10 solution, generic Session/Team/World Entity state, event-sourced commands, Marten/PostgreSQL persistence, projections, minimal REST endpoints, health checks, OpenAPI, JWT conventions, and a typed SignalR notification hub.

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

Then execute `src/Hambaft.Api/Hambaft.Api.http`. No Docker fallback exists.
