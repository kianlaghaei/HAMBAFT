import { create } from 'zustand'
import { createJSONStorage, persist } from 'zustand/middleware'
import type { VisualMode } from '../features/visual-world/types'

type SetupCode = { teamId: string; teamName: string; businessName: string; code: string }
type UiState = {
  selectedTeamTab: string
  reducedMotion: boolean
  visualMode: VisualMode
  audioEnabled: boolean
  setupCodes: Record<string, SetupCode[]>
  setTeamTab: (tab: string) => void
  setReducedMotion: (value: boolean) => void
  setVisualMode: (value: VisualMode) => void
  setAudioEnabled: (value: boolean) => void
  saveSetupCodes: (sessionId: string, codes: SetupCode[]) => void
}

export const useUiStore = create<UiState>()(persist((set) => ({
  selectedTeamTab: 'story',
  reducedMotion: typeof matchMedia !== 'undefined' && matchMedia('(prefers-reduced-motion: reduce)').matches,
  visualMode: 'high',
  audioEnabled: false,
  setupCodes: {},
  setTeamTab: (selectedTeamTab) => set({ selectedTeamTab }),
  setReducedMotion: (reducedMotion) => set({ reducedMotion }),
  setVisualMode: (visualMode) => set({ visualMode }),
  setAudioEnabled: (audioEnabled) => set({ audioEnabled }),
  saveSetupCodes: (sessionId, codes) => set((state) => ({ setupCodes: { ...state.setupCodes, [sessionId]: codes } })),
}), { name: 'hambaft-ui', storage: createJSONStorage(() => sessionStorage) }))
