# Frontend routes

| Route | Mode | Purpose |
| --- | --- | --- |
| `/` | Public | Entry and mode choice |
| `/pair` | Team bootstrap | Pairing code; redirects an already paired Team |
| `/team` | Team | Redirect to current story |
| `/team/story` | Team | Private Storylets and Choices |
| `/team/market` | Team | Public business overview |
| `/team/messages` | Team | Proposal inbox/outbox and negotiation |
| `/team/agreements` | Team | Party-visible Agreements |
| `/team/status` | Team | Team-visible metrics, Memories and Consequences |
| `/team/ending` | Team completed | Entity and World Ending |
| `/display/:sessionId` | PublicDisplay | Projector-safe public projection |
| `/admin` | Admin bootstrap | Session wizard or Development credential setup |
| `/admin/session/:sessionId` | Admin | Redirect to setup |
| `/admin/session/:sessionId/setup` | Admin | assignments, package lock and one-time codes |
| `/admin/session/:sessionId/runtime` | Admin | live checkpoint/ending controls and diagnostics |

Team and Admin route guards use unexpired role credentials. A page refresh restores credentials from `sessionStorage` and then refetches authoritative data. The Ending route redirects until the Team projection reports a completed Session. Public Display REST is intentionally unauthenticated under the current Backend contract; its SignalR connection obtains a Development-only PublicDisplay token locally.
