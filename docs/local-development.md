# Local development

HAMBAFT expects PostgreSQL to run as a normal local service. Do not use Docker.

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

Alternatively, expose the development connection as `ConnectionStrings__Hambaft`; it must target only `hambaft_dev`. Use `src/Hambaft.Api/Hambaft.Api.http` for the development smoke flow. Copy the raw pairing code only into the pairing request and do not log or persist it. The response JWT can be used for the SignalR negotiate request.

The API fails startup with a setup-oriented message when `ConnectionStrings:Hambaft` is missing. Integration tests reject any database name other than `hambaft_test`, reject SharedWorld explicitly, drop/recreate only schema `hambaft`, and never operate on `hambaft_dev`.
