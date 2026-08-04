import { render, screen } from '@testing-library/react'
import { expect, it } from 'vitest'
import { ProposalCard } from '../features/proposals/ProposalCard'
import { ids } from './fixtures'

it('renders typed Proposal terms and immutable Counterproposal revision history', () => {
  render(<ProposalCard targetName={(id) => id === ids.team ? 'سپیده' : 'راه‌نو'} proposal={{ proposalId: ids.proposal, interactionTypeId: 'emergency-supply', senderTeamId: ids.team, receiverTeamId: ids.otherTeam, currentRevisionNumber: 2, termsPayload: { units: 12, public: false, note: 'تا غروب' }, status: 'Countered', deadlineCheckpoint: 'shipment-missing', allowedActions: [], stateVersion: 14, revisions: [{ revisionNumber: 1, createdByTeamId: ids.team, termsPayload: { units: 8, public: true }, createdAtUtc: '2026-08-04T08:00:00Z' }, { revisionNumber: 2, createdByTeamId: ids.otherTeam, termsPayload: { units: 12, public: false, note: 'تا غروب' }, createdAtUtc: '2026-08-04T08:01:00Z' }] }} />)
  expect(screen.getAllByText('۱۲')).toHaveLength(2)
  expect(screen.getAllByText('خیر')).toHaveLength(2)
  expect(screen.getByText('تاریخچه تغییرناپذیر بازنگری‌ها')).toBeInTheDocument()
  expect(screen.getByText('بازنگری ۱')).toBeInTheDocument()
})
