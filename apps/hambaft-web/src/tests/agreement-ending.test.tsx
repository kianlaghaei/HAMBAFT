import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Outlet, Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { AgreementsPage } from '../features/agreements/AgreementsPage'
import { EndingView } from '../features/endings/EndingView'
import { completedExperience, ids, teamExperienceFixture } from './fixtures'

function withContext(node: React.ReactNode, experience = teamExperienceFixture) {
  return render(<QueryClientProvider client={new QueryClient()}><MemoryRouter><Routes><Route element={<Outlet context={{ experience, canWrite: true }} />}><Route index element={node} /></Route></Routes></MemoryRouter></QueryClientProvider>)
}

describe('agreements and endings', () => {
  it('renders only Agreements present in the Team-visible projection', () => {
    const experience = { ...teamExperienceFixture, sessionMetadata: null, agreements: [{ agreementId: ids.agreement, interactionTypeId: 'credit-guarantee', parties: [ids.team, ids.otherTeam], termsPayload: { units: 5, public: false }, status: 'Active', activationCheckpoint: 'morning-without-bell', executionCheckpoint: null, visibility: 'PrivateToParties' }] }
    withContext(<AgreementsPage />, experience)
    expect(screen.getByText('پیمان بازار')).toBeInTheDocument()
    expect(screen.getByText('خصوصی میان طرف‌ها')).toBeInTheDocument()
  })

  it('renders localized Ending narrative and traceable Backend evidence without win/loss labels', () => {
    render(<EndingView ending={completedExperience.entityEnding!} />)
    expect(screen.getByText('چراغ تنور ماند')).toBeInTheDocument()
    expect(screen.getByText(/یکی از تصمیم‌های ثبت‌شده/)).toBeInTheDocument()
    expect(screen.queryByText(/برد|باخت/)).not.toBeInTheDocument()
  })
})
