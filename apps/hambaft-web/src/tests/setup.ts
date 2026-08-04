import '@testing-library/jest-dom/vitest'
import { afterEach, beforeEach, vi } from 'vitest'
import { cleanup } from '@testing-library/react'
import { useAuthStore } from '../auth/authStore'

beforeEach(() => {
  sessionStorage.clear()
  useAuthStore.setState({ team: null, admin: null, displays: {} })
  vi.restoreAllMocks()
})

afterEach(() => cleanup())
