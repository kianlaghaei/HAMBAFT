import { useQuery } from '@tanstack/react-query'
import { api } from '../api/client'
import { queryKeys } from '../api/queryKeys'
import { useAuthStore } from '../auth/authStore'

export function useTeamExperience() {
  const token = useAuthStore((state) => state.team?.accessToken)
  return useQuery({ queryKey: queryKeys.teamExperience, queryFn: () => api.teamExperience(token ?? ''), enabled: Boolean(token) })
}

export function usePublicWorld(sessionId?: string) {
  return useQuery({ queryKey: queryKeys.publicWorld(sessionId ?? ''), queryFn: () => api.publicWorld(sessionId ?? ''), enabled: Boolean(sessionId) })
}

export function usePackage(id?: string, version?: string) {
  return useQuery({ queryKey: queryKeys.package(id ?? '', version ?? ''), queryFn: () => api.package(id ?? '', version ?? ''), enabled: Boolean(id && version), staleTime: Infinity })
}
