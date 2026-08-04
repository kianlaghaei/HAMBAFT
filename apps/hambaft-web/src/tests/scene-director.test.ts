import { describe, expect, it } from 'vitest'
import type { Proposal, PublicWorld, TeamExperience } from '../api/schemas'
import { buildSceneDescriptor, proposalVisualState } from '../features/visual-world/sceneDirector'
import { ids, publicWorldFixture, teamExperienceFixture } from './fixtures'

const thirdEntity = '30000000-0000-4000-8000-000000000003'
const fourthEntity = '30000000-0000-4000-8000-000000000004'
const baseEntity = publicWorldFixture.entities[0]!
const completeWorld: PublicWorld = {
  ...publicWorldFixture,
  storyVersion: '0.2.0',
  entities: [
    ...publicWorldFixture.entities,
    { ...baseEntity, id: thirdEntity, definitionId: 'printing-roshan', displayName: 'چاپخانه روشن', controlledByTeamId: null, controllerType: 'AuthoredBehavior' },
    { ...baseEntity, id: fourthEntity, definitionId: 'exchange-mizan', displayName: 'صرافی میزان', controlledByTeamId: null, controllerType: 'AuthoredBehavior' },
  ],
  narrative: { ...publicWorldFixture.narrative!, presentationTags: { scene: 'market-morning,missing-bell,ledger-discovery', time: 'morning', atmosphere: 'quiet,uncertain' } },
}

describe('Hezar Cheragh Scene Director', () => {
  it('maps authored tags and the four stable business locations without evaluating rules', () => {
    const scene = buildSceneDescriptor(completeWorld)
    expect(scene.locations.map((location) => location.id)).toEqual(['bakery-sepideh', 'logistics-rah-no', 'printing-roshan', 'exchange-mizan'])
    expect(scene.events).toEqual(expect.arrayContaining(['market-morning', 'missing-bell', 'ledger-discovery']))
    expect(scene.time).toBe('morning')
    expect(scene.uncontrolledEntityIds).toEqual([thirdEntity, fourthEntity])
  })

  it('only maps relationships present in the caller-authorized projection', () => {
    const publicRelationship = { sourceEntityId: ids.entity, targetEntityId: ids.otherEntity, relationshipKey: 'Trust', numericValue: 70 }
    const privateRelationship = { sourceEntityId: ids.entity, targetEntityId: ids.otherEntity, relationshipKey: 'Obligation', numericValue: 22 }
    const hiddenKind = { sourceEntityId: ids.entity, targetEntityId: ids.otherEntity, relationshipKey: 'HiddenLeverage', numericValue: 99 }
    const world = { ...completeWorld, publicRelationships: [publicRelationship, hiddenKind] }
    const team = { ...teamExperienceFixture, visibleRelationships: [publicRelationship, privateRelationship, hiddenKind] }
    expect(buildSceneDescriptor(world).connections.map((line) => line.kind)).toEqual(['trust'])
    expect(buildSceneDescriptor(world, team).connections.map((line) => line.kind)).toEqual(['trust', 'obligation'])
    expect(buildSceneDescriptor(world, team).connections[1]!.damaged).toBe(true)
  })

  it.each([
    ['Pending', 'sent'], ['Countered', 'countered'], ['Accepted', 'accepted'], ['Rejected', 'rejected'], ['Expired', 'expired'], ['Executed', 'executed'], ['Failed', 'failed'],
    [0, 'sent'], [1, 'countered'], [6, 'executed'], [7, 'failed'],
  ])('maps proposal status %s to %s', (status, expected) => expect(proposalVisualState(status)).toBe(expected))

  it('creates a return messenger and an accepted agreement from authoritative team data', () => {
    const proposal: Proposal = { proposalId: ids.proposal, interactionTypeId: 'emergency-supply', senderTeamId: ids.team, receiverTeamId: ids.otherTeam, currentRevisionNumber: 2, termsPayload: {}, status: 'Countered', deadlineCheckpoint: 'avan-offer', allowedActions: [], stateVersion: 14, revisions: [] }
    const team: TeamExperience = { ...teamExperienceFixture, inbox: [proposal], agreements: [{ agreementId: ids.agreement, interactionTypeId: 'emergency-supply', parties: [ids.team, ids.otherTeam], termsPayload: {}, status: 'Active', activationCheckpoint: 'morning-without-bell', executionCheckpoint: null, visibility: 'PrivateToParties' }] }
    const scene = buildSceneDescriptor(completeWorld, team)
    expect(scene.messengers[0]).toMatchObject({ state: 'countered', revision: 2 })
    expect(scene.connections).toContainEqual(expect.objectContaining({ kind: 'agreement', status: 'active' }))
  })

  it('restores the same scene from the same refreshed REST projection and changes on a new checkpoint', () => {
    expect(buildSceneDescriptor(structuredClone(completeWorld))).toEqual(buildSceneDescriptor(completeWorld))
    const next = buildSceneDescriptor({ ...completeWorld, currentCheckpointId: 'avan-offer', stateVersion: 20, narrative: { ...completeWorld.narrative!, presentationTags: { scene: 'avan-arrival', time: 'dusk', presence: 'avan' } } })
    expect(next.id).toContain('avan-offer')
    expect(next.avanVisible).toBe(true)
    expect(next.time).toBe('dusk')
  })
})
