# HAMBAFT agent guidance

- `../shared-world` (SharedWorld) is read-only. Never change it, commit in it, connect to its database, or reference its assemblies.
- Docker and container-based databases are out of scope. PostgreSQL runs locally as an operating-system service.
- Phase 4 provides compiled Ink presentation, deterministic Entity/World Endings with evidence, the `hezar-cheragh/0.1.0` vertical slice, frontend-ready projections, and retains all Phase 3 interaction, behavior, consequence and replay guarantees.
- Ink is presentation-only. It must never mutate authoritative state, execute Effects, resolve behavior/agreements/endings, or access persistence. Runtime loads compiled Ink JSON only.
- Resolve Entity Endings before the World Ending and append all Ending events plus Session completion atomically. Preserve structured evidence and privacy.
- Preserve Proposal revision immutability, Team privacy, exact Package ID/version/hash locks, stable Entity ordering and checkpoint-based (never wall-clock) expiration.
- Prefer simple code and a runnable build over speculative abstractions.
- Keep Domain generic; do not introduce market-specific or economic-specific concepts.
- Use UTF-8 and ensure user-facing Persian text is RTL-compatible. Keep code identifiers in English.
- Run `dotnet restore`, `dotnet build`, and `dotnet test` before handoff.
- Integration tests may access only `hambaft_test` and schema `hambaft`.
- Never commit credentials or connection strings.
- Do not push.
