export type VisualMode = 'high' | 'reduced' | 'fallback'

export type SceneTime = 'before-open' | 'morning' | 'near-noon' | 'afternoon' | 'dusk' | 'night'

export type SceneEvent =
  | 'market-morning'
  | 'missing-bell'
  | 'ledger-discovery'
  | 'northern-road-closure'
  | 'cargo-shortage'
  | 'rumour-spread'
  | 'avan-arrival'
  | 'night-pressure'
  | 'courtyard-gathering'
  | 'entity-ending'
  | 'world-ending'

export type ScenePoint = { x: number; y: number }

export type SceneLocation = ScenePoint & {
  id: string
  entityId?: string
  teamId?: string
  name: string
  shortIdentity: string
  whyItMatters: string
  currentCondition: string
  whoIsHere: string
  recentChange: string
  availableActions: string[]
  presentationTags: string[]
  state: 'quiet' | 'new-information' | 'action-available' | 'proposal-received' | 'agreement-active' | 'consequence-returned' | 'public-event' | 'changed-since-last-visit' | 'ending-relevant'
  businessKind: 'bakery' | 'logistics' | 'printing' | 'exchange' | 'business'
  controlled: boolean
  active: boolean
}

export type SceneConnection = {
  id: string
  fromEntityId: string
  toEntityId: string
  kind: 'trust' | 'obligation' | 'debt' | 'agreement'
  label: string
  damaged: boolean
  status?: 'active' | 'executed' | 'failed'
}

export type ProposalVisualState = 'sent' | 'countered' | 'accepted' | 'rejected' | 'expired' | 'executed' | 'failed'

export type SceneMessenger = {
  id: string
  fromEntityId: string
  toEntityId: string
  state: ProposalVisualState
  revision: number
}

export type SceneDescriptor = {
  id: string
  visualVersion: number
  time: SceneTime
  title: string
  timeLabel: string
  atmosphereLabel: string
  publicEvent: string
  courtyardActivity: string
  soundscape: string
  atmosphere: string[]
  events: SceneEvent[]
  locations: SceneLocation[]
  connections: SceneConnection[]
  messengers: SceneMessenger[]
  uncontrolledEntityIds: string[]
  avanVisible: boolean
  pressure: 'calm' | 'uneasy' | 'strained' | 'critical'
  pulse: string[]
  businessPulse: string[]
  ambientEvents: Array<{ id: string; description: string; locationId: string }>
  characters: Array<{ id: string; displayName: string; locationId: string; whatIsKnown: string; lastSeen: string; attitude: string; recentStatement: string; possibleInteraction?: string | null }>
  reactions: Array<{ outcomeLine: string; locationIds: string[]; visualTags: string[] }>
  semantic: boolean
  ending: 'none' | 'entity' | 'world'
}

export const marketLayers = [
  'background architecture',
  'lighting and time',
  'business locations',
  'ambient population',
  'messengers and movement',
  'relationships and agreements',
  'event effects',
  'foreground atmosphere',
  'camera and transitions',
] as const
