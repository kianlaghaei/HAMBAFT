# Story runtime

Phase 2 conditions and effects remain unchanged and generic. Phase 3 composes them into interactions, authored behavior and delayed consequences; it does not duplicate Storylet selection.

## Lifecycle

1. Create a Session locked to Package ID/version/hash, seed and Package-owned Difficulty.
2. Add Teams and Entities, assign HumanTeam Entities, then start.
3. Initialize the narrative and Package metrics/relationships; validate behavior profiles and difficulty.
4. Authenticated Teams submit choices and negotiate structured Proposals.
5. Admin resolves a checkpoint atomically using the documented Phase 3 pipeline.

## Deterministic checkpoint pipeline

1. Validate all required Team responses.
2. Expire due Proposals in stable Proposal ID order.
3. Execute checkpoint-mode Agreements in stable Agreement ID order.
4. Trigger due Scheduled Consequences in stable scheduled ID order.
5. Resolve AuthoredBehavior Entities in `Entity Definition ID → Entity ID` order.
6. Apply Team Choice effects in existing `priority desc → Assignment ID → Team ID → authored effect order` order.
7. Evaluate the resulting public narrative.
8. Select next Storylets.
9. Mark previous Assignments resolved.
10. Advance the checkpoint.
11. Append every event in one optimistic Marten transaction.

The implementation records the checkpoint transition before appending newly selected assignments, so steps 7–10 are represented by one `NarrativeCheckpointResolved` fact followed by deterministic assignment/publication facts in the same atomic batch. Any exception prevents the entire append.

## Privacy and replay

Team claims determine Session and Team identity. Public views contain only public memories, narrative, Agreements and consequences. Team views contain only own private Storylets, party Proposals/Agreements and visible pending consequences. Terms never travel through SignalR or structured logs. Event replay and projection rebuild reproduce behavior Actions, expiration, Agreement status, consequence status and fingerprint.
