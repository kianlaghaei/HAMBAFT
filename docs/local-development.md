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
