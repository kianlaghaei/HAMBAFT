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
Inline projections
```

HAMBAFT is an independent repository with no project or assembly reference to SharedWorld. PostgreSQL provides one local persistence technology and Marten provides an event store with optimistic stream concurrency. One authoritative stream per Session keeps the consistency boundary explicit and replay straightforward.

There is no runtime AI. Story content, Ink, storylets, choices, effects, authored behavior resolution and endings are deferred. The current design is intentionally minimal so a playable-test runtime can be continued without speculative infrastructure.
