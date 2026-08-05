# HAMBAFT agent guidance

## Product

- HAMBAFT is a Persian RTL multiplayer narrative game.
- Hezar Cheragh is the current primary game.
- React renders the UI.
- PixiJS renders the living map.
- The Backend is authoritative.

## Hard rules

- Never modify `../shared-world` (SharedWorld), reference its assemblies, or connect to its database.
- Never print credentials or connection strings.
- Never run tests against `hambaft_dev`.
- Never clean `hambaft_dev`; integration tests use only `hambaft_test` and schema `hambaft`.
- Do not move authoritative gameplay rules into React.
- Do not expose private Team data on Public Display.
- Do not overwrite published Story Package versions.
- Do not use runtime generative AI inside gameplay.
- Do not modify Backend behavior or Story Packages unless explicitly requested.
- Do not push unless explicitly requested.

## Current UI target

- Single-screen laptop gameplay.
- No document-level vertical scroll during active gameplay.
- Map, current event, and active action visible together.
- Persian-first and RTL-first.
- Player-facing raw metrics are hidden.
- Proposal and Agreement are part of the story.
- Fallback rendering and reduced motion remain functional.

## Commands

Run from the repository root:

```powershell
dotnet restore
dotnet build
dotnet test
```

Frontend checks:

```powershell
npm run typecheck --prefix apps/hambaft-web
npm run lint --prefix apps/hambaft-web
npm run test --prefix apps/hambaft-web
npm run build --prefix apps/hambaft-web
```

Local API:

```powershell
dotnet run --project src/Hambaft.Api
```

Playwright:

```powershell
npm run e2e --prefix apps/hambaft-web
```

RTL rules: [docs/RTL_GAME_UI_RULES.md](docs/RTL_GAME_UI_RULES.md)
