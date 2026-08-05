import { render, screen } from '@testing-library/react'
import type { ReactNode } from 'react'
import { MemoryRouter, Outlet, Route, Routes } from 'react-router-dom'
import { expect, it, vi } from 'vitest'
import { TeamShell } from '../features/team-experience/TeamShell'
import { publicWorldFixture, teamExperienceFixture } from './fixtures'

vi.mock('../hooks/useServerQuery', () => ({
  useTeamExperience: () => ({ data: { ...teamExperienceFixture, sessionMetadata: { ...teamExperienceFixture.sessionMetadata, storyPackageId: 'other-package' } }, isLoading: false, isError: false }),
  usePublicWorld: () => ({ data: publicWorldFixture }),
}))
vi.mock('../realtime/RealtimeBoundary', () => ({
  RealtimeBoundary: ({ children }: { children: ReactNode }) => <>{children}</>,
  ConnectionBadge: () => null,
  useConnectionStatus: () => 'connected',
}))

it('does not render the obsolete Team route navigation', () => {
  render(<MemoryRouter initialEntries={['/team']}><Routes><Route path="/team" element={<TeamShell />}><Route index element={<Outlet />} /></Route></Routes></MemoryRouter>)
  expect(screen.queryByRole('navigation', { name: 'بخش‌های بازی' })).not.toBeInTheDocument()
  expect(screen.queryByRole('link', { name: /روایت|بازار|پیام‌ها|پیمان‌ها|وضعیت حجره|سرانجام/ })).not.toBeInTheDocument()
})
