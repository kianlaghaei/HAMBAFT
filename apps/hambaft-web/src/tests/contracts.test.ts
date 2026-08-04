import { describe, expect, it } from 'vitest'
import { publicWorldSchema, teamExperienceSchema } from '../api/schemas'
import { publicWorldFixture, teamExperienceFixture } from './fixtures'

describe('Zod API contracts', () => {
  it('accepts a representative Team Experience projection', () => {
    expect(teamExperienceSchema.parse(teamExperienceFixture).privateStorylets[0]?.title).toBe('دفتر نیمه‌باز')
  })

  it('fails clearly when a required projection field is malformed', () => {
    expect(() => teamExperienceSchema.parse({ ...teamExperienceFixture, stateVersion: 'twelve' })).toThrow()
  })

  it('drops private extras at the Public Display boundary', () => {
    const parsed = publicWorldSchema.parse({ ...publicWorldFixture, privateStorylets: [{ paragraphs: ['راز سپیده'] }], privateProposals: [{ terms: 'secret' }] })
    expect(parsed).not.toHaveProperty('privateStorylets')
    expect(JSON.stringify(parsed)).not.toContain('راز سپیده')
  })
})
