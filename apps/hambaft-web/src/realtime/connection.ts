import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import type { QueryClient, QueryKey } from '@tanstack/react-query'
import { queryKeys } from '../api/queryKeys'

export type ConnectionStatus = 'connected' | 'reconnecting' | 'disconnected'

const teamEvents = new Set(['PrivateStoryAssigned', 'DecisionRequested', 'ChoiceRecorded', 'NarrativeResolved', 'ConsequenceChanged', 'EntityEndingPublished', 'WorldEndingPublished', 'SessionCompleted'])
const proposalEvents = new Set(['ProposalReceived', 'ProposalCountered', 'ProposalAccepted', 'ProposalRejected', 'ProposalExpired', 'ProposalCancelled'])
const agreementEvents = new Set(['AgreementActivated', 'AgreementExecuted', 'AgreementFailed'])
const publicEvents = new Set(['WorldNarrativePublished', 'NarrativeResolved', 'AuthoredBehaviorResolved', 'ConsequenceChanged', 'WorldEndingPublished', 'SessionCompleted'])

export function invalidationKeysForEvent(eventName: string, sessionId: string): QueryKey[] {
  const keys: QueryKey[] = []
  if (teamEvents.has(eventName)) keys.push(queryKeys.teamExperience)
  if (proposalEvents.has(eventName)) keys.push(queryKeys.inbox, queryKeys.outbox, queryKeys.teamExperience)
  if (agreementEvents.has(eventName)) keys.push(queryKeys.agreements, queryKeys.teamExperience, queryKeys.publicWorld(sessionId))
  if (publicEvents.has(eventName)) keys.push(queryKeys.publicWorld(sessionId), queryKeys.admin(sessionId))
  if (eventName === 'StateChanged') keys.push(queryKeys.session(sessionId), queryKeys.admin(sessionId), queryKeys.teamExperience, queryKeys.publicWorld(sessionId))
  else if (eventName === 'ChoiceRecorded') keys.push(queryKeys.session(sessionId), queryKeys.admin(sessionId))
  return keys
}

class ConnectionManager {
  private connection: HubConnection | null = null
  private status: ConnectionStatus = 'disconnected'
  private listeners = new Set<() => void>()

  subscribe = (listener: () => void) => { this.listeners.add(listener); return () => this.listeners.delete(listener) }
  snapshot = () => this.status
  private setStatus(status: ConnectionStatus) { this.status = status; this.listeners.forEach((listener) => listener()) }

  async connect(token: string, sessionId: string, client: QueryClient) {
    if (this.connection?.state === HubConnectionState.Connected) return
    await this.disconnect()
    const hubUrl = import.meta.env.VITE_SIGNALR_HUB_URL ?? `${(import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')}/hubs/session`
    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl || '/hubs/session', { accessTokenFactory: () => token })
      .withAutomaticReconnect([0, 1_000, 3_000, 8_000, 15_000])
      .configureLogging(import.meta.env.DEV ? LogLevel.Warning : LogLevel.Error)
      .build()
    this.connection = connection
    const events = [...new Set([...teamEvents, ...proposalEvents, ...agreementEvents, ...publicEvents, 'StateChanged', 'NarrativeInitialized'])]
    events.forEach((eventName) => connection.on(eventName, () => {
      invalidationKeysForEvent(eventName, sessionId).forEach((queryKey) => void client.invalidateQueries({ queryKey }))
    }))
    connection.onreconnecting(() => this.setStatus('reconnecting'))
    connection.onreconnected(() => {
      this.setStatus('connected')
      void client.invalidateQueries({ queryKey: queryKeys.session(sessionId) })
      void client.invalidateQueries({ queryKey: queryKeys.teamExperience })
      void client.invalidateQueries({ queryKey: queryKeys.publicWorld(sessionId) })
      void client.invalidateQueries({ queryKey: queryKeys.admin(sessionId) })
    })
    connection.onclose(() => this.setStatus('disconnected'))
    this.setStatus('reconnecting')
    try { await connection.start(); this.setStatus('connected') } catch { this.setStatus('disconnected') }
  }

  async disconnect() {
    const previous = this.connection
    this.connection = null
    if (previous && previous.state !== HubConnectionState.Disconnected) await previous.stop()
    this.setStatus('disconnected')
  }
}

export const connectionManager = new ConnectionManager()
