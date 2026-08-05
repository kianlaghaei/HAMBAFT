# Frontend routes

| Route | Mode | Purpose |
| --- | --- | --- |
| `/` | Public | Entry and mode choice |
| `/pair` | Team bootstrap | Pairing code; redirects an already paired Team |
| `/team` | Team | Canonical single-screen gameplay page |
| `/team/story` | Team legacy | Internal replace redirect to `/team` |
| `/team/market` | Team legacy | Internal replace redirect to `/team` |
| `/team/messages` | Team legacy | Internal replace redirect to `/team` |
| `/team/agreements` | Team legacy | Internal replace redirect to `/team` |
| `/team/status` | Team legacy | Internal replace redirect to `/team` |
| `/team/ending` | Team legacy | Internal replace redirect to `/team` |
| `/display/:sessionId` | PublicDisplay | Projector-safe public projection |
| `/admin` | Admin bootstrap | Session wizard or Development credential setup |
| `/admin/session/:sessionId` | Admin | Redirect to setup |
| `/admin/session/:sessionId/setup` | Admin | assignments, package lock and one-time codes |
| `/admin/session/:sessionId/runtime` | Admin | live checkpoint/ending controls and diagnostics |

Team and Admin route guards use unexpired role credentials. A page refresh restores credentials from `sessionStorage` and then refetches authoritative data. Obsolete Team URLs never render separate Team pages. Public Display REST is intentionally unauthenticated under the current Backend contract; its SignalR connection obtains a Development-only PublicDisplay token locally.
