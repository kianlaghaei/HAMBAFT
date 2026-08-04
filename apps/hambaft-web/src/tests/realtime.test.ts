import { expect, it } from 'vitest'
import { invalidationKeysForEvent } from '../realtime/connection'
import { ids } from './fixtures'

it('SignalR events invalidate only relevant authoritative REST projections', () => {
  expect(invalidationKeysForEvent('ProposalReceived', ids.session)).toEqual([['proposals', 'inbox'], ['proposals', 'outbox'], ['team-experience']])
  expect(invalidationKeysForEvent('WorldEndingPublished', ids.session)).toContainEqual(['public-world', ids.session])
  expect(invalidationKeysForEvent('PrivateStoryAssigned', ids.session)).not.toContainEqual(['public-world', ids.session])
})
