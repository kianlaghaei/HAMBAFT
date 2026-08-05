import { useSyncExternalStore } from 'react'

type Diagnostics = { activeCanvasCount: number; activeSceneId: string; ambientEntityCount: number; registeredRealtimeHandlers: number; fallbackState: boolean }
let state: Diagnostics = { activeCanvasCount: 0, activeSceneId: '', ambientEntityCount: 0, registeredRealtimeHandlers: 0, fallbackState: false }
const listeners = new Set<() => void>()
const emit = () => listeners.forEach((listener) => listener())
export const marketDiagnostics = {
  update(value: Partial<Diagnostics>) { state = { ...state, ...value }; emit() },
  snapshot: () => state,
  subscribe(listener: () => void) { listeners.add(listener); return () => listeners.delete(listener) },
}
export function useMarketDiagnostics() { return useSyncExternalStore(marketDiagnostics.subscribe, marketDiagnostics.snapshot, marketDiagnostics.snapshot) }
