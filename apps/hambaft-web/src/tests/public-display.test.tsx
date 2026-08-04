import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { expect, it } from 'vitest'
import { queryKeys } from '../api/queryKeys'
import { DisplayContent } from '../features/public-display/PublicDisplayPage'
import { ids, publicWorldFixture } from './fixtures'

it('Public Display renders public narrative and never renders injected Team-private content', () => {
  const client = new QueryClient()
  client.setQueryData(queryKeys.publicWorld(ids.session), { ...publicWorldFixture, privateStorylets: [{ paragraphs: ['راز خصوصی سپیده'] }], privateProposals: [{ note: 'قرارداد محرمانه' }] })
  render(<QueryClientProvider client={client}><DisplayContent sessionId={ids.session} /></QueryClientProvider>)
  expect(screen.getByText('امروز زنگ بازار به صدا درنیامد.')).toBeInTheDocument()
  expect(screen.queryByText('راز خصوصی سپیده')).not.toBeInTheDocument()
  expect(screen.queryByText('قرارداد محرمانه')).not.toBeInTheDocument()
})
