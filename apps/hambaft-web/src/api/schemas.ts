import { z } from 'zod'

export const guidSchema = z.uuid()
const enumValueSchema = z.union([z.string(), z.number()])

export const tokenSchema = z.object({
  accessToken: z.string().min(1),
  tokenType: z.string(),
  expiresAtUtc: z.iso.datetime({ offset: true }),
})

export const commandSchema = z.object({
  sessionId: guidSchema,
  stateVersion: z.number().int().nonnegative(),
  eventType: z.string(),
  resourceId: guidSchema.nullish(),
  pairingCode: z.string().nullish(),
})

export const entitySchema = z.object({
  id: guidSchema,
  sessionId: guidSchema,
  definitionId: z.string(),
  displayName: z.string(),
  controllerType: enumValueSchema,
  controlledByTeamId: guidSchema.nullish(),
  behaviorProfileId: z.string().nullish(),
  status: enumValueSchema,
})

export const teamSchema = z.object({
  id: guidSchema,
  sessionId: guidSchema,
  displayName: z.string(),
  controlledEntityId: guidSchema.nullish(),
  joinedAtUtc: z.string(),
})

export const metricSchema = z.object({
  scope: enumValueSchema,
  scopeId: guidSchema,
  metricKey: z.string(),
  numericValue: z.number(),
})

export const memorySchema = z.object({
  scope: enumValueSchema,
  scopeId: guidSchema,
  key: z.string(),
  optionalJsonValue: z.string().nullish(),
  visibility: enumValueSchema,
  createdAtUtc: z.string(),
})

const relationshipSchema = z.object({
  sourceEntityId: guidSchema,
  targetEntityId: guidSchema,
  relationshipKey: z.string(),
  numericValue: z.number(),
})

const narrativeChoiceSchema = z.object({ id: z.string(), label: z.string(), shortOutcome: z.string().nullish() })
const storyletSchema = z.object({
  assignmentId: guidSchema,
  storyletId: z.string(),
  checkpointId: z.string(),
  title: z.string(),
  paragraphs: z.array(z.string()),
  presentationTags: z.record(z.string(), z.string()),
  choices: z.array(narrativeChoiceSchema),
  requiredResponse: z.boolean(),
  submitted: z.boolean(),
  submittedChoiceId: z.string().nullish(),
})

const endingEvidenceSchema = z.object({
  kind: enumValueSchema,
  referenceId: guidSchema.nullish(),
  stableId: z.string().nullish(),
  key: z.string().nullish(),
  value: z.string().nullish(),
  isPublic: z.boolean(),
})

export const endingSchema = z.object({
  endingResultId: guidSchema,
  scope: enumValueSchema,
  scopeId: guidSchema,
  endingDefinitionId: z.string(),
  title: z.string(),
  paragraphs: z.array(z.string()),
  presentationTags: z.record(z.string(), z.string()),
  evidence: z.array(endingEvidenceSchema),
  contentHash: z.string(),
  resolvedAtStreamVersion: z.number().int(),
})

export const proposalRevisionSchema = z.object({
  revisionNumber: z.number().int().positive(),
  createdByTeamId: guidSchema,
  termsPayload: z.record(z.string(), z.unknown()),
  createdAtUtc: z.string(),
})

export const proposalSchema = z.object({
  proposalId: guidSchema,
  interactionTypeId: z.string(),
  senderTeamId: guidSchema,
  receiverTeamId: guidSchema,
  currentRevisionNumber: z.number().int().positive(),
  termsPayload: z.record(z.string(), z.unknown()),
  status: enumValueSchema,
  deadlineCheckpoint: z.string(),
  allowedActions: z.array(z.string()),
  stateVersion: z.number().int(),
  revisions: z.array(proposalRevisionSchema).nullish(),
})

export const agreementSchema = z.object({
  agreementId: guidSchema,
  interactionTypeId: z.string(),
  parties: z.array(guidSchema),
  termsPayload: z.record(z.string(), z.unknown()),
  status: enumValueSchema,
  activationCheckpoint: z.string(),
  executionCheckpoint: z.string().nullish(),
  visibility: enumValueSchema,
})

const consequenceSchema = z.object({
  scheduledConsequenceId: guidSchema,
  definitionId: z.string(),
  dueCheckpointId: z.string().nullish(),
  triggerType: enumValueSchema,
  status: enumValueSchema,
  visibility: enumValueSchema,
})

const sessionMetadataSchema = z.object({
  sessionId: guidSchema,
  status: enumValueSchema,
  storyPackageId: z.string(),
  storyVersion: z.string(),
  contentHash: z.string(),
  difficultyId: z.string(),
  currentCheckpointId: z.string().nullish(),
})

export const teamExperienceSchema = z.object({
  id: guidSchema,
  sessionId: guidSchema,
  team: teamSchema,
  controlledEntity: entitySchema.nullish(),
  visibleMetrics: z.array(metricSchema),
  visibleMemories: z.array(memorySchema),
  visibleRelationships: z.array(relationshipSchema),
  stateVersion: z.number().int().nonnegative(),
  currentCheckpointId: z.string().nullish(),
  privateStorylets: z.array(storyletSchema),
  inbox: z.array(proposalSchema).nullish(),
  outbox: z.array(proposalSchema).nullish(),
  agreements: z.array(agreementSchema).nullish(),
  availableInteractionTypes: z.array(z.object({ interactionTypeId: z.string(), allowedTargetTeamIds: z.array(guidSchema) })).nullish(),
  pendingConsequences: z.array(consequenceSchema).nullish(),
  entityEnding: endingSchema.nullish(),
  worldEnding: endingSchema.nullish(),
  sessionMetadata: sessionMetadataSchema.nullish(),
})

const narrativeSchema = z.object({
  storyletId: z.string(),
  narrativeRef: z.string(),
  title: z.string(),
  paragraphs: z.array(z.string()),
  presentationTags: z.record(z.string(), z.string()),
})

export const publicWorldSchema = z.object({
  id: guidSchema,
  status: enumValueSchema,
  entities: z.array(entitySchema),
  worldMetrics: z.array(metricSchema),
  publicMemories: z.array(memorySchema),
  publicRelationships: z.array(relationshipSchema),
  stateVersion: z.number().int(),
  storyPackageId: z.string(),
  storyVersion: z.string(),
  contentHash: z.string(),
  currentCheckpointId: z.string().nullish(),
  narrative: narrativeSchema.nullish(),
  publicAgreements: z.array(agreementSchema).nullish(),
  publicConsequences: z.array(consequenceSchema).nullish(),
  worldEnding: endingSchema.nullish(),
  publicEntityEndingSummaries: z.array(endingSchema).nullish(),
  difficultyId: z.string(),
})

export const sessionSchema = z.object({
  id: guidSchema,
  storyPackageId: z.string(),
  storyVersion: z.string(),
  contentHash: z.string(),
  seed: z.number().int(),
  status: z.string(),
  createdAtUtc: z.string(),
  startedAtUtc: z.string().nullish(),
  completedAtUtc: z.string().nullish(),
  teams: z.array(teamSchema),
  entities: z.array(entitySchema),
  stateVersion: z.number().int(),
})

export const adminSchema = z.object({
  id: guidSchema,
  teamsAwaitingResponse: z.array(guidSchema),
  assignedStoryletIds: z.array(z.string()),
  checkpoint: z.string().nullish(),
  openProposals: z.array(z.object({ proposalId: guidSchema, interactionTypeId: z.string(), status: enumValueSchema }).passthrough()),
  activeAgreements: z.array(z.object({ agreementId: guidSchema, interactionTypeId: z.string(), status: enumValueSchema }).passthrough()),
  pendingConsequences: z.array(consequenceSchema.passthrough()),
  uncontrolledBehaviorSelections: z.array(z.object({ entityId: guidSchema, actionId: z.string(), checkpointId: z.string() }).passthrough()),
  endingEligibilityDiagnostics: z.array(z.object({ endingDefinitionId: z.string(), eligible: z.boolean(), priority: z.number() }).passthrough()),
  packageVersion: z.string(),
  contentHash: z.string(),
  streamVersion: z.number().int(),
})

const termSchema: z.ZodType<{
  type: string
  name: string | null
  required: boolean
  minimum: number | null
  maximum: number | null
  maximumLength: number | null
  fields: Array<z.infer<typeof termSchema>>
}> = z.lazy(() => z.object({
  type: z.string(), name: z.string().nullable(), required: z.boolean(), minimum: z.number().nullable(), maximum: z.number().nullable(), maximumLength: z.number().int().nullable(), fields: z.array(termSchema),
}))

export const packageSchema = z.object({
  id: z.string(), version: z.string(), title: z.string(), description: z.string(), minimumTeams: z.number().int(), maximumTeams: z.number().int(), estimatedDurationMinutes: z.number().int(), defaultLocale: z.string(), contentHash: z.string(),
  entities: z.array(z.object({ id: z.string(), displayName: z.string(), controllerRequirement: z.string(), publicTags: z.array(z.string()) })),
  metrics: z.array(z.object({ key: z.string(), scope: z.string(), minimum: z.number(), maximum: z.number(), defaultValue: z.number() })),
  interactions: z.array(z.object({ id: z.string(), displayName: z.string(), description: z.string(), termsSchema: termSchema, defaultValidityType: z.string(), validUntilCheckpointId: z.string().nullable(), validForCheckpointCount: z.number().int().nullable(), executionMode: z.string(), agreementVisibility: z.string() })),
  difficulties: z.array(z.object({ id: z.string(), displayName: z.string() })),
  behaviorProfiles: z.array(z.object({ id: z.string(), displayName: z.string(), eligibleEntityDefinitionIds: z.array(z.string()) })),
})

export const packageCatalogSchema = z.array(z.object({
  id: z.string(), version: z.string(), title: z.string(), description: z.string(), minimumTeams: z.number().int(), maximumTeams: z.number().int(), estimatedDurationMinutes: z.number().int(), isValid: z.boolean(), contentHash: z.string(), errors: z.array(z.unknown()),
}))

export type TeamExperience = z.infer<typeof teamExperienceSchema>
export type PublicWorld = z.infer<typeof publicWorldSchema>
export type Session = z.infer<typeof sessionSchema>
export type AdminSession = z.infer<typeof adminSchema>
export type StoryPackage = z.infer<typeof packageSchema>
export type Proposal = z.infer<typeof proposalSchema>
export type Agreement = z.infer<typeof agreementSchema>
export type TermSchema = z.infer<typeof termSchema>
export type Token = z.infer<typeof tokenSchema>
export type CommandResponse = z.infer<typeof commandSchema>
export type PackageCatalog = z.infer<typeof packageCatalogSchema>
