# Story Package format

A Story Package is immutable, versioned repository content at `stories/{id}/{version}`. Phase 2 requires exactly these source files: `manifest.json`, `metrics.json`, `entities.json`, `storylets.json`, `effects.json`, and `narrative.json`. Extra files are also included in the content hash.

## Manifest

Required fields are `id`, `version`, `title`, `description`, `minimumTeams`, `maximumTeams`, `entryCheckpointId`, `defaultLocale`, `supportedLocales`, `estimatedDurationMinutes`, and `requiredRuntimeVersion`. Phase 2 accepts package major version 1 and runtime version `2.0`. Manifest ID/version must match the directory names.

## Definitions

- Metric: `key`, `scope`, `minimum`, `maximum`, `defaultValue`. Scopes are `World`, `Team`, `Entity`, `Relationship`.
- Entity: `id`, `displayName`, `controllerRequirement`, `publicTags`, `initialMetrics`. Requirements are `HumanTeam`, `AuthoredBehavior`, `System`, `Any`; Phase 2 does not decide for AuthoredBehavior.
- Storylet: `id`, `checkpointId`, `scope`, explicit `targetSelector`, `narrativeRef`, `conditions`, `choices`, `priority`, positive `weight`, `repeatPolicy`, `requiredResponse`, optional `nextCheckpointId`.
- Choice: `id`, `labelRef`, ordered `effectIds`, optional `nextStoryletHint`.
- Narrative: locale → stable key → authored `title`, `paragraphs`, optional `choiceLabel`, optional `shortOutcome`, and string `presentationTags`.

Selectors are only `World`, `AllTeams`, `TeamControllingEntityDefinition`, and `EntityDefinition`. Storylet scopes are `WorldPublic`, `TeamPrivate`, `EntityPrivate`, and `AdminOnly`. Repeat policies are `OncePerSession`, `OncePerCheckpoint`, and `Repeatable`.

## Canonical hash

Every file is sorted by normalized `/` relative path. JSON is parsed and re-emitted with object properties sorted ordinally and no formatting whitespace; arrays retain authored order. Non-JSON line endings normalize to LF. Lengths use fixed little-endian encoding and content is UTF-8. SHA-256 output is prefixed `sha256:`. Absolute paths, timestamps and generated hashes are never inputs and the hash is never written into source files.

Validation reports file path, content ID when available, code and human-readable message. It checks duplicates, required fields, versions, Team range, checkpoints, metric ranges/scopes, entity/storylet/narrative/effect/choice references, selectors, condition/effect shape, private/public leaks, eligible required targets and the mandatory sample-flow exit.
