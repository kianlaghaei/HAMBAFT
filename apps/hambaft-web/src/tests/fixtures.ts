import type { PublicWorld, TeamExperience } from '../api/schemas'

export const ids = {
  session: '10000000-0000-4000-8000-000000000001',
  team: '20000000-0000-4000-8000-000000000001',
  otherTeam: '20000000-0000-4000-8000-000000000002',
  entity: '30000000-0000-4000-8000-000000000001',
  otherEntity: '30000000-0000-4000-8000-000000000002',
  assignment: '40000000-0000-4000-8000-000000000001',
  proposal: '50000000-0000-4000-8000-000000000001',
  agreement: '60000000-0000-4000-8000-000000000001',
  ending: '70000000-0000-4000-8000-000000000001',
}

const entity = { id: ids.entity, sessionId: ids.session, definitionId: 'bakery-sepideh', displayName: 'نانوایی سپیده', controllerType: 'HumanTeam', controlledByTeamId: ids.team, behaviorProfileId: null, status: 'Active' }
const team = { id: ids.team, sessionId: ids.session, displayName: 'تیم سپیده', controlledEntityId: ids.entity, joinedAtUtc: '2026-08-04T08:00:00Z' }
const ending = { endingResultId: ids.ending, scope: 'Entity', scopeId: ids.entity, endingDefinitionId: 'steady-oven', title: 'چراغ تنور ماند', paragraphs: ['سپیده راهی ساخت که بازار آن را به یاد سپرد.'], presentationTags: { mood: 'warm' }, evidence: [{ kind: 'Choice', referenceId: ids.assignment, stableId: 'stable-price', key: null, value: null, isPublic: false }], contentHash: 'sha256:test', resolvedAtStreamVersion: 22 }

export const teamExperienceFixture: TeamExperience = {
  id: ids.team, sessionId: ids.session, team, controlledEntity: entity,
  visibleMetrics: [{ scope: 'Entity', scopeId: ids.entity, metricKey: 'Liquidity', numericValue: 42 }],
  visibleMemories: [], visibleRelationships: [], stateVersion: 12, currentCheckpointId: 'morning-without-bell',
  privateStorylets: [{ assignmentId: ids.assignment, storyletId: 'bakery-opening', checkpointId: 'morning-without-bell', title: 'دفتر نیمه‌باز', paragraphs: ['این روایت خصوصی سپیده است.'], presentationTags: { speaker: 'شاگرد نانوا' }, choices: [{ id: 'keep-price', label: 'قیمت را نگه دار', shortOutcome: null }], requiredResponse: true, submitted: false, submittedChoiceId: null }],
  inbox: [], outbox: [], agreements: [], availableInteractionTypes: [{ interactionTypeId: 'emergency-supply', allowedTargetTeamIds: [ids.otherTeam] }], pendingConsequences: [], entityEnding: null, worldEnding: null,
  sessionMetadata: { sessionId: ids.session, status: 'Running', storyPackageId: 'hezar-cheragh', storyVersion: '0.1.0', contentHash: 'sha256:test', difficultyId: 'standard', currentCheckpointId: 'morning-without-bell' },
}

export const completedExperience: TeamExperience = { ...teamExperienceFixture, privateStorylets: [], entityEnding: ending, worldEnding: { ...ending, endingResultId: '70000000-0000-4000-8000-000000000002', scope: 'World', scopeId: ids.session, title: 'بازار چراغش را نگه داشت', evidence: [] }, sessionMetadata: { ...teamExperienceFixture.sessionMetadata!, status: 'Completed' } }

export const publicWorldFixture: PublicWorld = {
  id: ids.session, status: 'Running', entities: [entity, { ...entity, id: ids.otherEntity, definitionId: 'logistics-rah-no', displayName: 'باربری راه‌نو', controlledByTeamId: ids.otherTeam }], worldMetrics: [], publicMemories: [], publicRelationships: [], stateVersion: 12,
  storyPackageId: 'hezar-cheragh', storyVersion: '0.1.0', contentHash: 'sha256:test', currentCheckpointId: 'morning-without-bell', narrative: { storyletId: 'world-opening', narrativeRef: 'world-opening', title: 'بازار بی‌زنگ', paragraphs: ['امروز زنگ بازار به صدا درنیامد.'], presentationTags: {} }, publicAgreements: [], publicConsequences: [], worldEnding: null, publicEntityEndingSummaries: [], difficultyId: 'standard',
}

export function jwt(role: 'Team' | 'Admin' | 'PublicDisplay', teamId?: string) {
  const encode = (value: object) => btoa(JSON.stringify(value)).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_')
  return `${encode({ alg: 'none' })}.${encode({ client_role: role, session_id: ids.session, team_id: teamId, exp: 4102444800 })}.signature`
}
