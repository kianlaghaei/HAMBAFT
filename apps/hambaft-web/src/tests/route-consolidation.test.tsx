import { render, screen } from '@testing-library/react'
import { MemoryRouter, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { App } from '../app/App'
import { useAuthStore } from '../auth/authStore'
import { ids, jwt } from './fixtures'

vi.mock('../features/team-experience/TeamShell', () => ({
  TeamShell: () => {
    const location = useLocation()
    const navigate = useNavigate()
    return <div data-testid="team-shell"><span data-testid="team-path">{location.pathname}</span><button type="button" data-testid="back" onClick={() => navigate(-1)}>back</button><Outlet /></div>
  },
}))
vi.mock('../features/narrative/StoryPage', () => ({ StoryPage: () => <div data-testid="team-gameplay-page">single Team page</div> }))
vi.mock('../features/admin/AdminHomePage', () => ({ AdminHomePage: () => <div data-testid="admin-home">admin home</div> }))
vi.mock('../features/public-display/PublicDisplayPage', () => ({ PublicDisplayPage: () => <div data-testid="public-display">public display</div> }))

const teamToken = { accessToken: jwt('Team', ids.team), tokenType: 'Bearer', expiresAtUtc: '2099-01-01T00:00:00+00:00' }

function renderApp(initialEntries: string[]) {
  return render(<MemoryRouter initialEntries={initialEntries}><App /></MemoryRouter>)
}

describe('Team single-page route consolidation', () => {
  beforeEach(() => { useAuthStore.getState().setToken(teamToken) })

  it.each(['/team/story', '/team/market', '/team/messages', '/team/agreements', '/team/status', '/team/ending'])('redirects obsolete Team route %s to canonical gameplay', async (route) => {
    renderApp([route])
    expect(await screen.findByTestId('team-gameplay-page')).toBeInTheDocument()
    expect(screen.getByTestId('team-path')).toHaveTextContent('/team')
  })

  it('renders the canonical Team gameplay route directly and restores it from root', async () => {
    renderApp(['/team'])
    expect(await screen.findByTestId('team-gameplay-page')).toBeInTheDocument()

    renderApp(['/'])
    expect(await screen.findAllByTestId('team-gameplay-page')).not.toHaveLength(0)
  })

  it('does not expose an obsolete page when browser Back crosses a legacy entry', async () => {
    renderApp(['/team', '/team/messages'])
    expect(await screen.findByTestId('team-gameplay-page')).toBeInTheDocument()
    screen.getAllByTestId('back').at(-1)?.click()
    expect(screen.getAllByTestId('team-path').at(-1)).toHaveTextContent('/team')
  })

  it('keeps Team users out of Admin, including private Admin URLs', async () => {
    renderApp(['/admin/session/session-1/runtime'])
    expect(await screen.findByTestId('team-gameplay-page')).toBeInTheDocument()
    expect(screen.queryByTestId('admin-home')).not.toBeInTheDocument()
  })
})

describe('unpaired and non-Team route boundaries', () => {
  beforeEach(() => useAuthStore.setState({ team: null, admin: null, displays: {} }))

  it('sends unauthenticated Team access to pairing', async () => {
    renderApp(['/team'])
    expect(await screen.findByLabelText('کد جفت‌شدن')).toBeInTheDocument()
  })

  it('leaves Admin entry and Public Display routes available', async () => {
    renderApp(['/admin'])
    expect(await screen.findByTestId('admin-home')).toBeInTheDocument()
    renderApp([`/display/${ids.session}`])
    expect(await screen.findByTestId('public-display')).toBeInTheDocument()
  })
})
