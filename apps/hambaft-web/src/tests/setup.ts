import '@testing-library/jest-dom/vitest'
import { afterEach, beforeEach, vi } from 'vitest'
import { cleanup } from '@testing-library/react'
import { useAuthStore } from '../auth/authStore'
import { useUiStore } from '../state/uiStore'

beforeEach(() => {
  sessionStorage.clear()
  useAuthStore.setState({ team: null, admin: null, displays: {} })
  useUiStore.setState({ selectedTeamTab: 'story', reducedMotion: false, visualMode: 'fallback', audioEnabled: false, setupCodes: {} })
  vi.restoreAllMocks()
})

afterEach(() => cleanup())
