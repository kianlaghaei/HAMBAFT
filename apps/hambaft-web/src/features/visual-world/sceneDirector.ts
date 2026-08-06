import type { Agreement, Proposal, PublicWorld, TeamExperience } from '../../api/schemas'
import type { ProposalVisualState, SceneConnection, SceneDescriptor, SceneEvent, SceneLocation, SceneTime } from './types'

const checkpointEvents: Record<string, SceneEvent[]> = {
  'morning-without-bell': ['market-morning', 'missing-bell', 'ledger-discovery'],
  'cargo-did-not-arrive': ['northern-road-closure', 'cargo-shortage', 'rumour-spread'],
  'avan-offer': ['avan-arrival'], 'market-gathering': ['courtyard-gathering'], 'slice-complete': ['night-pressure'],
}
const knownEvents = new Set<SceneEvent>(['market-morning', 'missing-bell', 'ledger-discovery', 'northern-road-closure', 'cargo-shortage', 'rumour-spread', 'avan-arrival', 'night-pressure', 'courtyard-gathering', 'entity-ending', 'world-ending'])

export function proposalVisualState(status: string | number): ProposalVisualState {
  const names: Record<string, ProposalVisualState> = { Pending: 'sent', Countered: 'countered', Accepted: 'accepted', Rejected: 'rejected', Expired: 'expired', Executed: 'executed', Failed: 'failed', Cancelled: 'rejected' }
  const numbers: Record<string, ProposalVisualState> = { '0': 'sent', '1': 'countered', '2': 'accepted', '3': 'rejected', '4': 'rejected', '5': 'expired', '6': 'executed', '7': 'failed' }
  return names[String(status)] ?? numbers[String(status)] ?? 'sent'
}

function values(tags: Record<string, string> | undefined, key: string) {
  return (tags?.[key] ?? '').split(/[|,]/).map((value) => value.trim().toLowerCase().replace(/\s+/g, '-')).filter(Boolean)
}

function tagsFrom(world: PublicWorld, team?: TeamExperience) {
  const publicTags = world.narrative?.presentationTags ?? {}
  const privateTags = team?.privateStorylets.find((storylet) => !storylet.submitted)?.presentationTags ?? team?.privateStorylets[0]?.presentationTags ?? {}
  return team ? { ...publicTags, ...privateTags } : publicTags
}

function entityForTeam(world: PublicWorld, teamId: string) { return world.entities.find((entity) => entity.controlledByTeamId === teamId)?.id }

function agreementConnection(world: PublicWorld, agreement: Agreement): SceneConnection | null {
  const [first, second] = agreement.parties.map((teamId) => entityForTeam(world, teamId))
  if (!first || !second) return null
  const statusName = String(agreement.status)
  const status = ['Executed', '1'].includes(statusName) ? 'executed' : ['Failed', 'Cancelled', '2', '3'].includes(statusName) ? 'failed' : 'active'
  return { id: `agreement:${agreement.agreementId}`, fromEntityId: first, toEntityId: second, kind: 'agreement', label: 'تعهد فعال', damaged: status === 'failed', status }
}

function relationshipConnections(team?: TeamExperience): SceneConnection[] {
  return (team?.relationshipPresentation ?? []).map((relationship) => ({
    id: `relationship:${relationship.sourceEntityId}:${relationship.targetEntityId}:${relationship.relationshipKey}`,
    fromEntityId: relationship.sourceEntityId, toEntityId: relationship.targetEntityId,
    kind: relationship.relationshipKey === 'Trust' ? 'trust' : relationship.relationshipKey === 'Obligation' ? 'obligation' : 'debt',
    label: relationship.label,
    damaged: relationship.visualTags.some((tag) => tag.includes('broken') || tag.includes('damaged') || tag.includes('faded')),
  }))
}

function proposalMessengers(world: PublicWorld, proposals: Proposal[]) {
  return proposals.flatMap((proposal) => {
    const from = entityForTeam(world, proposal.senderTeamId); const to = entityForTeam(world, proposal.receiverTeamId)
    return from && to ? [{ id: proposal.proposalId, fromEntityId: from, toEntityId: to, state: proposalVisualState(proposal.status), revision: proposal.currentRevisionNumber }] : []
  })
}

function semanticLocations(world: PublicWorld, team?: TeamExperience): SceneLocation[] {
  const presentation = team?.worldPresentation ?? world.worldPresentation
  if (!presentation) return []
  const inboxTargets = new Set((team?.inbox ?? []).map((proposal) => entityForTeam(world, proposal.senderTeamId)))
  const agreementEntities = new Set((team?.agreements ?? []).flatMap((agreement) => agreement.parties.map((id) => entityForTeam(world, id))))
  const changed = new Set(presentation.reactions.flatMap((reaction) => reaction.locationIds))
  return presentation.locations.map((location) => {
    const entity = location.entityId ? world.entities.find((item) => item.id === location.entityId) : undefined
    const state: SceneLocation['state'] = world.worldEnding ? 'ending-relevant' : location.entityId && inboxTargets.has(location.entityId) ? 'proposal-received' : location.entityId && agreementEntities.has(location.entityId) ? 'agreement-active' : changed.has(location.id) ? 'changed-since-last-visit' : location.availableActions.length ? 'action-available' : location.presentationTags.includes('public-gathering') ? 'public-event' : 'quiet'
    return { id: location.id, entityId: location.entityId ?? undefined, teamId: entity?.controlledByTeamId ?? undefined, name: location.displayName, businessIdentity: location.businessIdentity, shortIdentity: location.shortIdentity, whyItMatters: location.whyItMatters, currentCondition: location.currentCondition, whoIsHere: location.whoIsHere, recentChange: location.recentChange, availableActions: location.availableActions, presentationTags: location.presentationTags, state, businessKind: businessKind(location.presentationTags), x: location.x * 10, y: location.y * 7, controlled: entity?.id === team?.controlledEntity?.id, active: entity ? !['Inactive', 'Closed', '2', '3'].includes(String(entity.status)) : true }
  })
}

function businessKind(tags: string[]): SceneLocation['businessKind'] {
  return (['bakery', 'logistics', 'printing', 'exchange'] as const).find((kind) => tags.includes(kind)) ?? 'business'
}

function sceneTime(value: string | undefined): SceneTime {
  if (value === 'noon') return 'near-noon'
  return ['before-open', 'morning', 'near-noon', 'afternoon', 'dusk', 'night'].includes(value ?? '') ? value as SceneTime : 'morning'
}

export function buildSceneDescriptor(world: PublicWorld, team?: TeamExperience): SceneDescriptor {
  const checkpoint = team?.currentCheckpointId ?? world.currentCheckpointId ?? 'waiting'
  const presentation = team?.worldPresentation ?? world.worldPresentation
  const tags = tagsFrom(world, team)
  const taggedEvents = [...values(tags, 'scene'), ...values(tags, 'event')].filter((event): event is SceneEvent => knownEvents.has(event as SceneEvent))
  const ending = world.worldEnding ? 'world' : team?.entityEnding ? 'entity' : 'none'
  const endingEvents: SceneEvent[] = ending === 'world' ? ['world-ending'] : ending === 'entity' ? ['entity-ending'] : []
  const events = [...new Set([...(checkpointEvents[checkpoint] ?? []), ...taggedEvents, ...endingEvents])]
  const proposals = [...(team?.inbox ?? []), ...(team?.outbox ?? [])].filter((proposal, index, all) => all.findIndex((candidate) => candidate.proposalId === proposal.proposalId) === index)
  const agreements = [...(world.publicAgreements ?? []), ...(team?.agreements ?? [])].filter((agreement, index, all) => all.findIndex((candidate) => candidate.agreementId === agreement.agreementId) === index).flatMap((agreement) => agreementConnection(world, agreement) ?? [])
  const pressure = presentation?.semanticMetrics.find((metric) => metric.key === 'Pressure')?.bandId
  return {
    id: `${world.id}:${presentation?.sceneId ?? checkpoint}`, visualVersion: Math.max(world.stateVersion, team?.stateVersion ?? 0), time: sceneTime(presentation?.timeOfDay ?? values(tags, 'time')[0]), title: world.narrative?.title ?? presentation?.atmosphereLabel ?? checkpoint,
    timeLabel: presentation?.timeLabel ?? 'صبح', atmosphereLabel: presentation?.atmosphereLabel ?? 'بازار هزارچراغ', publicEvent: presentation?.publicEvent ?? '', courtyardActivity: presentation?.courtyardActivity ?? '', soundscape: presentation?.soundscape ?? 'ambient-market', atmosphere: presentation?.visualTags ?? [...values(tags, 'atmosphere'), ...values(tags, 'mood')], events,
    locations: semanticLocations(world, team), connections: [...relationshipConnections(team), ...agreements], messengers: proposalMessengers(world, proposals), uncontrolledEntityIds: world.entities.filter((entity) => !['HumanTeam', '0'].includes(String(entity.controllerType))).map((entity) => entity.id), avanVisible: presentation?.avanVisible ?? (events.includes('avan-arrival') || values(tags, 'presence').includes('avan')), pressure: ['calm', 'uneasy', 'strained', 'critical'].includes(pressure ?? '') ? pressure as SceneDescriptor['pressure'] : 'calm', pulse: presentation?.pulse ?? [], businessPulse: team?.businessPresentation?.pulse ?? [], ambientEvents: presentation?.ambientEvents ?? [], characters: presentation?.characters ?? [], reactions: presentation?.reactions ?? [], semantic: Boolean(presentation), ending,
  }
}
