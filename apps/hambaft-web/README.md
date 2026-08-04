# HAMBAFT Web

React/TypeScript playable client for the HAMBAFT `hezar-cheragh/0.1.0` vertical slice.

## Commands

```powershell
npm install --prefix apps/hambaft-web
npm run dev --prefix apps/hambaft-web
npm run typecheck --prefix apps/hambaft-web
npm run lint --prefix apps/hambaft-web
npm run test --prefix apps/hambaft-web
npm run build --prefix apps/hambaft-web
npm run e2e --prefix apps/hambaft-web
```

The development server is `http://localhost:5173` and proxies `/api`, `/hubs`, health endpoints and Development-only token provisioning to `http://localhost:5297`. A production build is written to `src/Hambaft.Api/wwwroot`; starting the API then serves the SPA and API from one URL.

Optional runtime variables:

```text
VITE_API_BASE_URL
VITE_SIGNALR_HUB_URL
VITE_DEFAULT_LOCALE
```

Empty values use same-origin paths and work with both the Vite proxy and the ASP.NET-hosted build. Tokens are held only in `sessionStorage`; Team exit removes the Team credential.

## Contract strategy

The current ASP.NET OpenAPI document describes request schemas but does not emit concrete response schemas for most minimal API endpoints. Therefore Phase 5 keeps manually derived contracts in `src/api/schemas.ts`, validates security-sensitive projections with Zod, and covers representative/malformed responses with contract tests. HTTP calls and RFC Problem Details mapping remain centralized in `src/api/client.ts`.

No server projection is copied into Zustand. TanStack Query owns server state; Zustand contains credentials, one-time setup codes and local UI preferences only.

## Dependency audit note

As of 2026-08-04, `npm audit` reports two high-severity vulnerable package entries (`react-router` and `react-router-dom`) caused by one React Router advisory for RSC action handling. This client is a browser-only Vite SPA and does not use React Server Components or server actions. The project remains pinned to the newest compatible React Router release; tested older releases retain other overlapping advisories, so a forced downgrade is not safer. Recheck the audit when the upstream patched release becomes available.
