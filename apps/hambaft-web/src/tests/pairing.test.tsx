import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { PairingPage } from '../features/pairing/PairingPage'
import { ids, jwt, teamExperienceFixture } from './fixtures'

const renderPage = () => render(<QueryClientProvider client={new QueryClient()}><MemoryRouter initialEntries={['/pair']}><Routes><Route path="/pair" element={<PairingPage />} /><Route path="/team" element={<div>بازار تیم</div>} /></Routes></MemoryRouter></QueryClientProvider>)

describe('pairing', () => {
  it('stores a valid Team JWT and enters the Team experience', async () => {
    const token = { accessToken: jwt('Team', ids.team), tokenType: 'Bearer', expiresAtUtc: '2099-01-01T00:00:00+00:00' }
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(new Response(JSON.stringify(token), { status: 200 })).mockResolvedValueOnce(new Response(JSON.stringify(teamExperienceFixture), { status: 200 })))
    renderPage(); const user = userEvent.setup()
    await user.type(screen.getByLabelText('کد جفت‌شدن'), 'ABCD2345')
    await user.click(screen.getByRole('button', { name: 'ورود به بازار' }))
    expect(await screen.findByText('بازار تیم')).toBeInTheDocument()
  })

  it('shows the same generic error for invalid or expired codes', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('', { status: 401 })))
    renderPage(); const user = userEvent.setup()
    await user.type(screen.getByLabelText('کد جفت‌شدن'), 'BADCODE2')
    await user.click(screen.getByRole('button', { name: 'ورود به بازار' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('معتبر نیست یا زمان استفاده از آن گذشته')
  })
})
