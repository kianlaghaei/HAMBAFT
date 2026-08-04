export type VisualMode = 'high' | 'reduced' | 'fallback'

export type SceneTime = 'morning' | 'noon' | 'dusk' | 'night'

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
  entityId: string
  teamId?: string
  name: string
  businessKind: 'bakery' | 'logistics' | 'printing' | 'exchange' | 'business'
  controlled: boolean
  active: boolean
}

export type SceneConnection = {
  id: string
  fromEntityId: string
  toEntityId: string
  kind: 'trust' | 'obligation' | 'debt' | 'agreement'
  strength: number
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
  atmosphere: string[]
  events: SceneEvent[]
  locations: SceneLocation[]
  connections: SceneConnection[]
  messengers: SceneMessenger[]
  uncontrolledEntityIds: string[]
  avanVisible: boolean
  pressure: number
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
