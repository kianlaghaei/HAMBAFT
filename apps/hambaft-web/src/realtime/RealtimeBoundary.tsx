import { useEffect, useSyncExternalStore, type PropsWithChildren } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { connectionManager } from './connection'

export function RealtimeBoundary({ token, sessionId, children }: PropsWithChildren<{ token?: string; sessionId?: string }>) {
  const client = useQueryClient()
  useEffect(() => {
    if (!token || !sessionId) return
    const connect = () => void connectionManager.connect(token, sessionId, client)
    const disconnect = () => void connectionManager.disconnect()
    connect()
    window.addEventListener('online', connect)
    window.addEventListener('offline', disconnect)
    return () => {
      window.removeEventListener('online', connect)
      window.removeEventListener('offline', disconnect)
      disconnect()
    }
  }, [client, sessionId, token])
  return children
}

export function useConnectionStatus() {
  return useSyncExternalStore(connectionManager.subscribe, connectionManager.snapshot, connectionManager.snapshot)
}

export function ConnectionBadge() {
  const status = useConnectionStatus()
  const labels = { connected: 'متصل', reconnecting: 'در حال اتصال مجدد', disconnected: 'ارتباط قطع است' }
  return <span className={`connection connection--${status}`} role="status" aria-live="polite"><span aria-hidden="true" />{labels[status]}</span>
}
