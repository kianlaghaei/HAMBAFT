# Frontend realtime

`src/realtime/connection.ts` owns the only SignalR connection. It uses `accessTokenFactory`, automatic reconnect and a subscribable three-state connection status. Route-level boundaries start it once for Team/Admin/PublicDisplay and stop it on mode exit. Browser `offline` stops the connection immediately; `online` reconnects and refetches.

SignalR payloads are never rendered as state. Events map to the narrowest practical TanStack Query invalidations:

| Event family | Invalidations |
| --- | --- |
| private narrative / Choice / Entity Ending | Team Experience |
| Proposal | Team Experience, inbox, outbox |
| Agreement | Team Experience, Agreements, Public World |
| world narrative / consequence / behavior / World Ending | Public World and Admin |
| generic StateChanged | Session, Admin, Team Experience, Public World |

After reconnect the manager invalidates Session, Team Experience, Public World and Admin for the active Session. The current projection remains visible while disconnected and write controls are disabled. A new privacy-safe `StateChanged` notification after any Choice lets other Teams learn the new stream version without revealing the submitting Team's choice.
