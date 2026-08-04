# Architecture

```text
REST command
    ↓
Application handler
    ↓
StorySession aggregate
    ↓
Domain events
    ↓
Marten Session stream
    ↓
Inline Session / Public / SessionExperience projections
```

HAMBAFT is an independent repository with no project or assembly reference to SharedWorld. PostgreSQL provides one local persistence technology and Marten provides an event store with optimistic stream concurrency. One authoritative stream per Session keeps the consistency boundary explicit and replay straightforward. Session and Public views are direct projections. The replayable SessionExperience projection holds the generic state needed to materialize Team- and Entity-scoped views without imperative document writes.

The Team pairing code is returned once when the Team is created; only its PBKDF2 hash is event-stored. A successful pairing exchange issues a short-lived JWT scoped by `client_role`, `session_id`, and `team_id`. SignalR validates that token before accepting negotiation and selects all group names on the server from validated claims.

There is no runtime AI. Story content, Ink, storylets, choices, effects, authored behavior resolution and endings are deferred. The current design is intentionally minimal so a playable-test runtime can be continued without speculative infrastructure.
