Original prompt: Redesign the HAMBAFT Hezar Cheragh Team gameplay route as a real fixed single-screen RTL React/PixiJS game board, using the supplied reference image for hierarchy; preserve Backend authority; integrate map, event, objective, dominant action, Pacts, Agreements, world reactions, fallback, reduced motion, tablet bottom sheet, tests, screenshots, focused commits, and do not push.

## Working notes

- Active workspace: `C:\DevHub\02_Work\Clients\khedmat\HAMBAFT` (`D:` is unavailable in this environment).
- Active branch: `sprint24/best-playable-build` at initial HEAD `233dbee`.
- Preserve Backend authority, Proposal/Agreement APIs, package immutability, and all unrelated work.
- Required final evidence: no-scroll at 1366×768 and 1440×900, full requested screenshot set, test counts, database name, commits, and no push.

## TODO

- [x] Audit current Team route, shell, renderer, proposal/agreement flow, assets, tests, and visual lab.
- [x] Reuse the later production TeamGameplayScreen foundation on the requested Sprint 24 branch.
- [x] Implement three-column desktop shell, compact business panel, persistent event/action panel, semantic header and fixed dock.
- [x] Integrate modular registry assets into Pixi with graphics/SVG fallback; validated native dimensions and transparent pixels.
- [x] Add spatial Pact target → authored letter → typed terms → validity → seal/send presentation using the existing API.
- [x] Add tablet three-state bottom sheet and location-focused world reaction presentation.
- Add browser layout coverage and capture review artifacts.
- Run full Backend/frontend/Playwright verification and create focused local commits.

## Verification notes

- `npm run typecheck --prefix apps/hambaft-web`: passed.
- `npm run lint --prefix apps/hambaft-web`: passed.
- First targeted Vitest command used a repository-relative filter from the package working directory; runner found no tests. Re-run with `src/tests/...`.
