# Legacy infrastructure review

The implementation files and tests were inspected directly. Gameplay Domain code was not copied.

| Legacy path | Legacy symbol | Decision | HAMBAFT destination | Legacy assumptions removed |
| ----------- | ------------- | -------- | ------------------- | -------------------------- |
| `backend/SharedWorld.Backend.sln` | solution/project layout | Adapted | `Hambaft.sln`, `src/`, `tests/` | Independent repository and assembly graph |
| `backend/Directory.Build.props` | common build properties | Adapted | `Directory.Build.props` | SharedWorld naming and frontend coupling |
| `backend/Directory.Packages.props` | central package management | Adapted | `Directory.Packages.props` | EF Core, SQL Server and legacy packages |
| `backend/src/SharedWorld.Infrastructure/Security/JwtTokenService.cs` | `JwtTokenService` | Rewritten from pattern | `Hambaft.Infrastructure/Security` | Country/device/admin model replaced by generic client roles and Team claims |
| `backend/src/SharedWorld.Infrastructure/Security/PairingCodeService.cs` | `PairingCodeService` | Rewritten from pattern | `Hambaft.Infrastructure/Security` | Country/device persistence and EF transaction removed |
| `backend/src/SharedWorld.Application/Security/SecurityAbstractions.cs` | claims, policies, problem exception | Adapted | `Hambaft.Application/Security` | Country and participant concepts removed |
| `backend/src/SharedWorld.Api/Security/CurrentActorAccessor.cs` | claim-bound actor | Rewritten from pattern | deferred authenticated team query boundary | Arbitrary team IDs will never be trusted |
| `backend/src/SharedWorld.Api/Security/ActorTokenValidationEvents.cs` | SignalR bearer extraction | Adapted | `Hambaft.Api/Program.cs` | Device revocation database model removed |
| `backend/src/SharedWorld.Api/Realtime/GameHub.cs` | server-selected groups | Adapted | `Hambaft.Api/Realtime/SessionHub.cs` | Game/country/device groups replaced by session/team/client-role groups |
| `backend/src/SharedWorld.Api/Middleware/ProblemDetailsMiddleware.cs` | `ProblemDetailsMiddleware` | Rewritten from pattern | `Hambaft.Api/Middleware/ProblemDetailsMiddleware.cs` | SQL Server and EF exceptions removed; Marten concurrency conflict added |
| `backend/src/SharedWorld.Api/Program.cs` | correlation, health, OpenAPI, auth composition | Adapted | `Hambaft.Api/Program.cs` | SQL Server, static frontend, rate-limit and legacy routes removed |
| `backend/src/SharedWorld.Infrastructure/DependencyInjection.cs` | structured logging and health registration | Rewritten from pattern | `Hambaft.Infrastructure/DependencyInjection.cs` | EF/SQL Server and legacy seed services removed |
| `backend/src/SharedWorld.Infrastructure/GameRuntime/GameServiceSupport.cs` | secure random codes, SHA-256, state-version conflict | Rewritten from pattern | Domain fingerprinting and pairing services | Country code transliteration and mutable EF state removed |
| `backend/tests/SharedWorld.UnitTests/ArchitectureTests.cs` | assembly boundary test | Adapted | `Hambaft.ArchitectureTests` | Stronger independent-project and forbidden-symbol checks |
| `backend/tests/SharedWorld.IntegrationTests/SqlServerApiFactory.cs` | test host/config guard | Rewritten from pattern | `Hambaft.IntegrationTests` | SQL Server, dynamic database deletion and SharedWorld seeding rejected |
| `backend/src/SharedWorld.Domain/Entities.cs` | gameplay Domain model | Not reused | none | Entire legacy gameplay and economic model excluded |
