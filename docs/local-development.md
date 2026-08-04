# Local development

HAMBAFT uses the local PostgreSQL operating-system service. Docker and container databases are out of scope.

```powershell
createdb hambaft_dev
createdb hambaft_test

dotnet user-secrets init --project src/Hambaft.Api
dotnet user-secrets set "ConnectionStrings:Hambaft" "Host=localhost;Database=hambaft_dev;Username=<user>;Password=<password>" --project src/Hambaft.Api
dotnet user-secrets set "Jwt:SigningKey" "<at-least-32-random-characters>" --project src/Hambaft.Api
dotnet user-secrets set "Jwt:Issuer" "Hambaft" --project src/Hambaft.Api
dotnet user-secrets set "Jwt:Audience" "Hambaft.Clients" --project src/Hambaft.Api
$env:ConnectionStrings__HambaftTest = "Host=localhost;Database=hambaft_test;Username=<user>;Password=<password>"

dotnet restore
dotnet build
dotnet test
dotnet run --project src/Hambaft.Api
```

`ConnectionStrings__Hambaft` may replace the development user secret and must target `hambaft_dev`. Integration tests enforce database `hambaft_test` and schema `hambaft`; they reject other database names and SharedWorld.

Development defaults the Story Package root to repository folder `stories`. Override it with `StoryPackages__Root` when necessary. `src/Hambaft.Api/Hambaft.Api.http` lists both sample versions and walks the full 1.1.0 Proposal, counter, Agreement, expiration, Behavior and Consequence flow. Copy the 1.1.0 hash, pairing codes, scoped Team JWTs, deployment-provisioned Admin JWT, assignment/Proposal IDs and each returned state version into later requests. Never log or persist raw pairing codes, JWTs or Proposal terms.

The `/internal/.../bootstrap` state endpoint remains Development-only. No unrestricted production endpoint mutates metrics.
# Phase 4 local workflow

PostgreSQL remains a local operating-system service. Tests use only database `hambaft_test` and schema `hambaft`; API smoke uses `hambaft_dev`. Keep both connection strings in environment configuration and never place them in source or command output.

Compile edited Ink explicitly in Development, then verify the package hash changes:

```powershell
dotnet run --project src/Hambaft.Ink.Compiler -- stories/hezar-cheragh/0.1.0/narrative/hezar-cheragh.ink stories/hezar-cheragh/0.1.0/narrative/compiled/hezar-cheragh.ink.json
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Hambaft.Api
```

Use `src/Hambaft.Api/Hambaft.Api.http` for the 2-Team walkthrough. Admin/PublicDisplay tokens are deployment-provisioned; integration tests issue them through `JwtTokenIssuer` inside the test host. Do not add a production token-minting endpoint.

# Phase 5 React workflow

Keep the API on `http://localhost:5297`, then use a second terminal:

```powershell
npm install --prefix apps/hambaft-web
npm run dev --prefix apps/hambaft-web
```

Open `http://localhost:5173/admin`. Vite proxies REST, SignalR, health and Development-only auth endpoints. Optional overrides are `VITE_API_BASE_URL`, `VITE_SIGNALR_HUB_URL`, and `VITE_DEFAULT_LOCALE`; defaults work without `.env`. Never place JWTs or secrets in these values.

Admin setup creates a Session, then requests a Session-bound token from Development-only `/internal/auth/admin`. Public Display REST follows the existing unauthenticated public projection; its Development SignalR connection obtains `/internal/auth/public-display`. Neither token-provisioning endpoint exists outside Development.

For a one-URL local demo:

```powershell
npm run build --prefix apps/hambaft-web
dotnet run --project src/Hambaft.Api
```

Open `http://localhost:5297`. Vite emits to the ignored `src/Hambaft.Api/wwwroot`, and ASP.NET fallback routing restores nested SPA routes after refresh.

Frontend verification:

```powershell
npm run typecheck --prefix apps/hambaft-web
npm run lint --prefix apps/hambaft-web
npm run test --prefix apps/hambaft-web
npm run build --prefix apps/hambaft-web
npm run e2e --prefix apps/hambaft-web
```

Playwright uses the installed stable Chrome channel. The bundled Chromium CDN returned a regional HTTP 403 in the Phase 5 environment; the full Chrome run remains automated and verified.
