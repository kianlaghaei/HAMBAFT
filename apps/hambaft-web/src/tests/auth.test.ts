import { expect, it } from 'vitest'
import { credentialFromToken, useAuthStore } from '../auth/authStore'
import { ids, jwt } from './fixtures'

it('derives Team identity from JWT and unpair clears all Team authentication state', () => {
  const token = { accessToken: jwt('Team', ids.team), tokenType: 'Bearer', expiresAtUtc: '2099-01-01T00:00:00+00:00' }
  const credential = credentialFromToken(token)
  expect(credential.teamId).toBe(ids.team)
  useAuthStore.getState().setToken(token)
  useAuthStore.getState().unpair()
  expect(useAuthStore.getState().team).toBeNull()
  expect(sessionStorage.getItem('hambaft-auth')).not.toContain(token.accessToken)
})
