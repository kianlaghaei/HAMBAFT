import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Outlet, Route, Routes } from 'react-router-dom'
import { expect, it, vi } from 'vitest'
import { StoryPage } from '../features/narrative/StoryPage'
import { useAuthStore } from '../auth/authStore'
import { ids, jwt, teamExperienceFixture } from './fixtures'

it('renders only the Team private narrative and submits a Choice with version and command id', async () => {
  useAuthStore.setState({ team: { accessToken: jwt('Team', ids.team), tokenType: 'Bearer', expiresAtUtc: '2099-01-01T00:00:00+00:00', role: 'Team', sessionId: ids.session, teamId: ids.team } })
  const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ sessionId: ids.session, stateVersion: 13, eventType: 'StoryChoiceSubmitted', resourceId: null, pairingCode: null }), { status: 200 }))
  vi.stubGlobal('fetch', fetchMock)
  render(<QueryClientProvider client={new QueryClient()}><MemoryRouter initialEntries={['/']}><Routes><Route element={<Outlet context={{ experience: teamExperienceFixture, canWrite: true }} />}><Route index element={<StoryPage />} /></Route></Routes></MemoryRouter></QueryClientProvider>)
  expect(screen.getByText('این روایت خصوصی سپیده است.')).toBeInTheDocument()
  await userEvent.click(screen.getByRole('button', { name: /قیمت را نگه دار/ }))
  await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1))
  const init = fetchMock.mock.calls[0]?.[1] as RequestInit
  const body = JSON.parse(String(init.body)) as Record<string, unknown>
  expect(body).toMatchObject({ assignmentId: ids.assignment, choiceId: 'keep-price', expectedStateVersion: 12 })
  expect(body.commandId).toEqual(expect.any(String))
})
