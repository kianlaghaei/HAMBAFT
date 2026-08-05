# Semantic presentation system

Phase 6B keeps numbers authoritative in Domain events, metrics, Conditions, Effects and Ending calculations. `hezar-cheragh/0.3.0/presentation` is the only source of player-facing meaning.

## Mapping pipeline

```text
authoritative Metric + package scope/key
  -> validated complete, contiguous band
  -> localized label + description + visualTags
  -> World/Business/RelationshipPresentationState
  -> Team/Public React rendering
```

The package loader reads every presentation JSON file before validation. Validation rejects unknown metric/entity/choice/checkpoint/location references, duplicate presentation IDs, incomplete ranges, overlaps/gaps at authored integer boundaries, and empty invalid bands. The deterministic package hash already includes every nested file, so presentation changes require a new version/hash.

For `0.3.0`, Team `visibleMetrics` and Public `worldMetrics` are empty after semantic hydration. Team receives its own business pulse and authorized semantic relationships. Public receives world bands and scene-authored public business conditions only. Public relationships remain empty; public Agreements are a separate explicit contract. Old packages without semantic content retain their previous projection contract for replay compatibility.

## Distinct visual languages

- Pressure: crowd, queue, conversation, messenger and shutter tags.
- PublicTrust: approach distance, notices, body orientation and reaction quality.
- Autonomy: independent signs, Avan presence, branding and route markers.
- Transparency: ledgers, documents, verified notices and rumour markers.
- Resilience: recovery, shared carts/supplies, paths and mutual aid.

Business bands use only the Team-authorized entity metrics. Relationship bands produce localized states such as «رابطه گرم»، «تعهد فعال» and «بدهی حل‌نشده»; raw strength never enters React. Trend is part of the projection contract and currently defaults to `steady` until prior-band snapshots are retained by a future projection revision.

## Privacy boundary

Semantic projection cannot upgrade visibility. Team mappings receive only the metrics and relationships already authorized for that Team. Public mapping receives only world metrics and scene-authored public content. Ambient event selection receives public tags only and cannot infer a private consequence, business metric, character fact or agreement.
