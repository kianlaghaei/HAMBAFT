# Interactions runtime

## Proposal state machine

```text
Pending ─counter→ Countered ─counter→ Countered
   │                    │
   ├─accept─────────────┴─accept→ Accepted → Agreement Active → Executed/Failed
   ├─reject──────────────────────→ Rejected
   ├─cancel──────────────────────→ Cancelled
   └─checkpoint deadline─────────→ Expired
```

`ProposalRevision` is append-only. Revision 1 is emitted by `ProposalSent`; every counter is exactly prior revision + 1 and retains old terms. The creator of the current revision may cancel while waiting. The opposite party may accept, counter or reject. An accepted revision cannot change.

Terms are validated recursively against Package-authored schemas. Requests never contain an authoritative sender; JWT `session_id` and `team_id` claims supply identity. Receiver eligibility requires the same Session and matching selectors. Package Difficulty may author deterministic numeric minimum multipliers.

Validity is checkpoint-based. `ValidForCheckpointCount: 1` expires before mechanics execute at the current checkpoint resolution. Wall-clock time is not authoritative.

All commands require `expectedStateVersion` and `commandId`. The Session stream rejects Accept/Counter, Accept/Reject, Cancel/Accept, Expire/Accept and simultaneous counters with HTTP 409 when they race.

Acceptance emits `ProposalAccepted` and `AgreementActivated`. Immediate Agreements execute in that batch; checkpoint Agreements execute during resolution; manual Agreements require the runtime command and a party. Public Agreements appear in PublicWorld; private Agreements only in party views. Proposals never appear publicly.

REST endpoints are `/api/proposals/inbox`, `/api/proposals/outbox`, `/api/proposals`, the four proposal action routes, and `/api/agreements`. SignalR sends only Session/Proposal/Agreement IDs, state version and event type to affected `team:{id}` groups; public Agreement events also use `session:{id}` and `display:{id}`.

The replayable full Session state is readable only through JWT-bound `GET /api/sessions/{sessionId}/admin`; the existing public Session response remains safe and excludes proposal terms.
# Hezar Cheragh interactions

The vertical slice exercises the existing immutable Proposal runtime through `emergency-supply`, `credit-guarantee`, `evidence-exchange`, `public-announcement` and `shared-purchase`. The reference E2E sends revision 1, counters into immutable revision 2, accepts the current revision, executes the resulting Agreement and lets a separate checkpoint-bound Proposal expire. Ending evidence refers to Proposal/Agreement IDs and interaction types; Ink never creates or accepts an Agreement.
