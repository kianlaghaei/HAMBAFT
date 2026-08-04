# HAMBAFT agent guidance

- `../shared-world` (SharedWorld) is read-only. Never change it, commit in it, connect to its database, or reference its assemblies.
- Docker and container-based databases are out of scope. PostgreSQL runs locally as an operating-system service.
- Phase 2 provides repository Story Packages, typed conditions/effects, deterministic Storylet assignment, authored choices and replayable narrative checkpoints. Ink, behavior resolution and endings remain deferred.
- Prefer simple code and a runnable build over speculative abstractions.
- Keep Domain generic; do not introduce market-specific or economic-specific concepts.
- Use UTF-8 and ensure user-facing Persian text is RTL-compatible. Keep code identifiers in English.
- Run `dotnet restore`, `dotnet build`, and `dotnet test` before handoff.
- Integration tests may access only `hambaft_test` and schema `hambaft`.
- Never commit credentials or connection strings.
- Do not push.
