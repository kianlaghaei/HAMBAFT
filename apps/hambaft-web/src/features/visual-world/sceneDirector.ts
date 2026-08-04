import type { Agreement, Proposal, PublicWorld, TeamExperience } from '../../api/schemas'
import type { ProposalVisualState, SceneConnection, SceneDescriptor, SceneEvent, SceneLocation, ScenePoint, SceneTime } from './types'

const positions: Record<string, ScenePoint> = {
  'bakery-sepideh': { x: 220, y: 220 },
  'logistics-rah-no': { x: 775, y: 235 },
  'printing-roshan': { x: 235, y: 515 },
  'exchange-mizan': { x: 760, y: 510 },
}

const businessKinds: Record<string, SceneLocation['businessKind']> = {
  'bakery-sepideh': 'bakery',
  'logistics-rah-no': 'logistics',
  'printing-roshan': 'printing',
  'exchange-mizan': 'exchange',
}

const checkpointScenes: Record<string, { time: SceneTime; events: SceneEvent[]; atmosphere: string[] }> = {
  'morning-without-bell': { time: 'morning', events: ['market-morning', 'missing-bell', 'ledger-discovery'], atmosphere: ['quiet', 'uncertain'] },
  'cargo-did-not-arrive': { time: 'noon', events: ['northern-road-closure', 'cargo-shortage', 'rumour-spread'], atmosphere: ['shortage', 'rumour'] },
  'shipment-missing': { time: 'noon', events: ['northern-road-closure', 'cargo-shortage', 'rumour-spread'], atmosphere: ['shortage', 'rumour'] },
  'avan-offer': { time: 'dusk', events: ['avan-arrival'], atmosphere: ['pressure', 'watchful'] },
  'market-gathering': { time: 'night', events: ['night-pressure', 'courtyard-gathering'], atmosphere: ['pressure', 'crowded'] },
  'slice-complete': { time: 'night', events: ['courtyard-gathering'], atmosphere: ['resolved'] },
}

const knownEvents = new Set<SceneEvent>([
  'market-morning', 'missing-bell', 'ledger-discovery', 'northern-road-closure', 'cargo-shortage', 'rumour-spread',
  'avan-arrival', 'night-pressure', 'courtyard-gathering', 'entity-ending', 'world-ending',
])

const statusNumber: Record<string, string> = { '0': 'sent', '1': 'countered', '2': 'accepted', '3': 'rejected', '4': 'expired', '5': 'rejected', '6': 'executed', '7': 'failed' }
const statusName: Record<string, ProposalVisualState> = {
  Pending: 'sent', Sent: 'sent', Countered: 'countered', Accepted: 'accepted', Rejected: 'rejected', Expired: 'expired', Cancelled: 'rejected', Executed: 'executed', Failed: 'failed',
}

export function proposalVisualState(status: string | number): ProposalVisualState {
  const key = String(status)
  return statusName[key] ?? (statusNumber[key] as ProposalVisualState | undefined) ?? 'sent'
}

function enumName(value: string | number, names: Record<string, string>) {
  return names[String(value)] ?? String(value)
}

function values(tags: Record<string, string> | undefined, key: string) {
  return (tags?.[key] ?? '').split(/[|,]/).map((value) => value.trim().toLowerCase().replace(/\s+/g, '-')).filter(Boolean)
}

function tagsFrom(world: PublicWorld, team?: TeamExperience) {
  const publicTags = world.narrative?.presentationTags ?? {}
  const privateTags = team?.privateStorylets.find((storylet) => !storylet.submitted)?.presentationTags ?? team?.privateStorylets[0]?.presentationTags ?? {}
  return team ? { ...publicTags, ...privateTags } : publicTags
}

function entityForTeam(world: PublicWorld, teamId: string) {
  return world.entities.find((entity) => entity.controlledByTeamId === teamId)?.id
}

function agreementConnection(world: PublicWorld, agreement: Agreement): SceneConnection | null {
  const [first, second] = agreement.parties.map((teamId) => entityForTeam(world, teamId))
  if (!first || !second) return null
  const status = enumName(agreement.status, { '0': 'active', '1': 'executed', '2': 'failed', '3': 'failed', Active: 'active', Executed: 'executed', Failed: 'failed', Cancelled: 'failed' }) as 'active' | 'executed' | 'failed'
  return { id: `agreement:${agreement.agreementId}`, fromEntityId: first, toEntityId: second, kind: 'agreement', strength: status === 'failed' ? 20 : 100, damaged: status === 'failed', status }
}

function visibleRelationshipConnections(world: PublicWorld, team?: TeamExperience): SceneConnection[] {
  const source = team?.visibleRelationships ?? world.publicRelationships
  const permitted = new Set(['Trust', 'Obligation', 'DebtExposure', 'DebtObligation'])
  return source.filter((relationship) => permitted.has(relationship.relationshipKey)).map((relationship) => {
    const kind = relationship.relationshipKey === 'Trust' ? 'trust' : relationship.relationshipKey === 'Obligation' ? 'obligation' : 'debt'
    return {
      id: `relationship:${relationship.sourceEntityId}:${relationship.targetEntityId}:${relationship.relationshipKey}`,
      fromEntityId: relationship.sourceEntityId,
      toEntityId: relationship.targetEntityId,
      kind,
      strength: Math.max(0, Math.min(100, relationship.numericValue)),
      damaged: relationship.numericValue < 35,
    }
  })
}

function proposalMessengers(world: PublicWorld, proposals: Proposal[]) {
  return proposals.flatMap((proposal) => {
    const from = entityForTeam(world, proposal.senderTeamId)
    const to = entityForTeam(world, proposal.receiverTeamId)
    return from && to ? [{ id: proposal.proposalId, fromEntityId: from, toEntityId: to, state: proposalVisualState(proposal.status), revision: proposal.currentRevisionNumber }] : []
  })
}

export function buildSceneDescriptor(world: PublicWorld, team?: TeamExperience): SceneDescriptor {
  const checkpoint = team?.currentCheckpointId ?? world.currentCheckpointId ?? 'waiting'
  const authored = checkpointScenes[checkpoint] ?? { time: 'morning' as const, events: ['market-morning'] as SceneEvent[], atmosphere: ['waiting'] }
  const tags = tagsFrom(world, team)
  const taggedTime = values(tags, 'time')[0] as SceneTime | undefined
  const taggedEvents = [...values(tags, 'scene'), ...values(tags, 'event')].filter((event): event is SceneEvent => knownEvents.has(event as SceneEvent))
  const atmosphere = [...new Set([...authored.atmosphere, ...values(tags, 'atmosphere'), ...values(tags, 'mood')])]
  const ending = world.worldEnding ? 'world' : team?.entityEnding ? 'entity' : 'none'
  const endingEvent: SceneEvent[] = ending === 'world' ? ['world-ending'] : ending === 'entity' ? ['entity-ending'] : []
  const events = [...new Set([...authored.events, ...taggedEvents, ...endingEvent])]
  const locations = world.entities.map((entity, index): SceneLocation => {
    const fallback = [{ x: 220, y: 220 }, { x: 775, y: 235 }, { x: 235, y: 515 }, { x: 760, y: 510 }][index] ?? { x: 500, y: 360 }
    return {
      id: entity.definitionId,
      entityId: entity.id,
      teamId: entity.controlledByTeamId ?? undefined,
      name: entity.displayName,
      businessKind: businessKinds[entity.definitionId] ?? 'business',
      ...(positions[entity.definitionId] ?? fallback),
      controlled: entity.id === team?.controlledEntity?.id,
      active: !['Inactive', 'Closed', '2', '3'].includes(String(entity.status)),
    }
  })
  const agreements = [...(world.publicAgreements ?? []), ...(team?.agreements ?? [])]
    .filter((agreement, index, all) => all.findIndex((candidate) => candidate.agreementId === agreement.agreementId) === index)
    .flatMap((agreement) => agreementConnection(world, agreement) ?? [])
  const proposals = [...(team?.inbox ?? []), ...(team?.outbox ?? [])]
    .filter((proposal, index, all) => all.findIndex((candidate) => candidate.proposalId === proposal.proposalId) === index)
  const pressureMetric = world.worldMetrics.find((metric) => metric.metricKey === 'Pressure')?.numericValue ?? 0
  const uncontrolled = world.entities.filter((entity) => !['HumanTeam', '0'].includes(String(entity.controllerType))).map((entity) => entity.id)
  return {
    id: `${world.id}:${checkpoint}`,
    visualVersion: Math.max(world.stateVersion, team?.stateVersion ?? 0),
    time: ['morning', 'noon', 'dusk', 'night'].includes(taggedTime ?? '') ? taggedTime! : authored.time,
    title: world.narrative?.title ?? checkpoint,
    atmosphere,
    events,
    locations,
    connections: [...visibleRelationshipConnections(world, team), ...agreements],
    messengers: proposalMessengers(world, proposals),
    uncontrolledEntityIds: uncontrolled,
    avanVisible: events.includes('avan-arrival') || values(tags, 'presence').includes('avan'),
    pressure: Math.max(pressureMetric, atmosphere.includes('pressure') ? 60 : 0),
    ending,
  }
}
