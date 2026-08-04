import { create } from 'zustand'
import { createJSONStorage, persist } from 'zustand/middleware'
import type { Token } from '../api/schemas'

export type ClientRole = 'Team' | 'Admin' | 'PublicDisplay'
export type Credential = Token & { role: ClientRole; sessionId: string; teamId?: string }

type JwtPayload = { client_role?: string; session_id?: string; team_id?: string; exp?: number }

export function credentialFromToken(token: Token): Credential {
  const part = token.accessToken.split('.')[1]
  if (!part) throw new Error('Malformed JWT')
  const normalized = part.replace(/-/g, '+').replace(/_/g, '/')
  const payload = JSON.parse(atob(normalized)) as JwtPayload
  if (!payload.session_id || !['Team', 'Admin', 'PublicDisplay'].includes(payload.client_role ?? '')) throw new Error('Missing identity claims')
  return { ...token, role: payload.client_role as ClientRole, sessionId: payload.session_id, teamId: payload.team_id }
}

type AuthState = {
  team: Credential | null
  admin: Credential | null
  displays: Record<string, Credential>
  setToken: (token: Token) => Credential
  unpair: () => void
  clearAdmin: () => void
  clearDisplay: (sessionId: string) => void
}

export const useAuthStore = create<AuthState>()(persist((set) => ({
  team: null,
  admin: null,
  displays: {},
  setToken: (token) => {
    const credential = credentialFromToken(token)
    if (credential.role === 'Team') set({ team: credential })
    if (credential.role === 'Admin') set({ admin: credential })
    if (credential.role === 'PublicDisplay') set((state) => ({ displays: { ...state.displays, [credential.sessionId]: credential } }))
    return credential
  },
  unpair: () => set({ team: null }),
  clearAdmin: () => set({ admin: null }),
  clearDisplay: (sessionId) => set((state) => {
    const displays = { ...state.displays }
    delete displays[sessionId]
    return { displays }
  }),
}), {
  name: 'hambaft-auth',
  storage: createJSONStorage(() => sessionStorage),
  partialize: ({ team, admin, displays }) => ({ team, admin, displays }),
}))

export function isCredentialValid(value: Credential | null | undefined) {
  return Boolean(value && Date.parse(value.expiresAtUtc) > Date.now())
}
