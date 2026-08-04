# Architecture

```text
Repository Story Package files
        ↓ canonical validation + SHA-256 lock
REST command → Application runtime → StorySession aggregate
                                      ↓
                              meaningful Domain events
                                      ↓
                              Marten Session stream
                                      ↓
                  inline Session / Public / Experience projections
                                      ↓
                   package-hash-checked narrative hydration
```

HAMBAFT is independent and has no reference to SharedWorld. One Marten stream per Session is the optimistic-concurrency and atomic-resolution boundary. Aggregate state contains stable content IDs and runtime facts, never localized paragraphs. Projection documents are replayed from events; the filesystem package locked by ID, version and hash supplies localized narrative at query time. Hydration fails on a package hash mismatch.

The Domain remains generic. Sample-specific metric names and entity definitions exist only under `stories/sample-cargo-delay`. Conditions are immutable reads. Effects create meaningful fact events and never write projection documents directly. Numeric effects use one policy: clamp to the declared metric minimum/maximum.

Private Storylet assignments carry a target Team/Entity ID. Team views filter before localization; public views contain only `WorldPublic` publication IDs. SignalR sends only routing metadata and clients refetch REST projections.

The Team pairing code is returned once and only its PBKDF2 hash is stored. Team identity for choice submission comes from JWT claims. Admin/PublicDisplay credentials remain deployment-provisioned.

Deferred architecture: Ink, proposals, agreements, Behavior Resolver, difficulty, scheduled consequences, entity/world endings, React and full Hezar Cheragh content.
