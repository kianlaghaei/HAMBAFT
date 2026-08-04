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

Development defaults the Story Package root to repository folder `stories`. Override it with `StoryPackages__Root` when necessary. Run `src/Hambaft.Api/Hambaft.Api.http`: copy the sample hash, pairing codes, scoped Team JWTs, deployment-provisioned Admin JWT, assignment IDs and returned state versions into subsequent requests. Never log or persist raw pairing codes or JWTs.

The `/internal/.../bootstrap` state endpoint remains Development-only. No unrestricted production endpoint mutates metrics.
