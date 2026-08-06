import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { z } from 'zod'
import { api } from '../../api/client'
import { queryKeys } from '../../api/queryKeys'
import type { PublicWorld, TeamExperience, TermSchema } from '../../api/schemas'
import { useAuthStore } from '../../auth/authStore'
import { agreementStatusLabel, checkpointLabel, proposalStatusLabel } from '../../design-system/presentation'
import { TermsEditor, type TermDraft } from '../../components/TermsView'
import { EmptyMarketStage } from './EmptyMarketStage'
import { BusinessActionCard, DecisionSummary, LocationSceneSheet, MarketReactionCard, SceneSheet, StoryOpeningCard, TeamSceneUnavailableState, type ContextualPactPresentation, type TeamScenePresentation, type TeamStoryPhase } from './TeamStoryPresentation'
import { buildSceneDescriptor } from '../visual-world/sceneDirector'
import { useUiStore } from '../../state/uiStore'

type Storylet = TeamExperience['privateStorylets'][number]
type UtilityPanel = 'messages' | 'pacts' | null

export function TeamGameplayScreen({ experience, world, canWrite }: { experience: TeamExperience; world: PublicWorld; canWrite: boolean }) {
  const token = useAuthStore((state) => state.team?.accessToken ?? '')
  const client = useQueryClient()
  const descriptor = useMemo(() => buildSceneDescriptor(world, experience), [experience, world])
  const teamScene = experience.teamScenePresentation
  const storylet = experience.privateStorylets.find((item) => !item.submitted) ?? experience.privateStorylets[0]
  const sceneKey = `${experience.sessionMetadata?.storyPackageId ?? world.storyPackageId}:${experience.sessionMetadata?.storyVersion ?? world.storyVersion}:${world.id}:${experience.team.id}:${experience.controlledEntity?.id ?? 'unassigned'}:${teamScene?.sceneId ?? experience.currentCheckpointId ?? world.currentCheckpointId ?? storylet?.checkpointId ?? 'waiting'}`
  const [sceneProgress, setSceneProgress] = useState<{ key: string; selectedLocationId?: string; investigatedIds: string[] }>({ key: '', investigatedIds: [] })
  const [utilityPanel, setUtilityPanel] = useState<UtilityPanel>(null)
  const [pactTargetTeamId, setPactTargetTeamId] = useState('')
  const [interactionId, setInteractionId] = useState('')
  const [terms, setTerms] = useState<TermDraft>({})
  const [validForCheckpoints, setValidForCheckpoints] = useState(1)
  const [storyPhase, setStoryPhase] = useState<TeamStoryPhase>(storylet?.submitted ? 'reaction' : 'opening')
  const [chosenChoiceId, setChosenChoiceId] = useState<string>()
  const [reactionIndex, setReactionIndex] = useState(-1)
  const [sceneSheetOpen, setSceneSheetOpen] = useState(true)
  const [reviewedCommunication, setReviewedCommunication] = useState(false)
  const [notice, setNotice] = useState('')
  const [isContinuing, setIsContinuing] = useState(false)
  const seenReactionVersion = useUiStore((state) => state.seenReactionVersions[world.id])
  const markReactionSeen = useUiStore((state) => state.markReactionSeen)

  const persistedInvestigatedIds = teamScene?.investigatedLocationIds ?? []
  const currentProgress = sceneProgress.key === sceneKey
    ? { ...sceneProgress, investigatedIds: [...new Set([...persistedInvestigatedIds, ...sceneProgress.investigatedIds])] }
    : { key: sceneKey, investigatedIds: persistedInvestigatedIds }
  const selectedLocationId = currentProgress.selectedLocationId
  const requiredInvestigationCount = teamScene?.requiredInvestigationCount ?? 0
  const setProgressSelectedLocation = (id: string) => setSceneProgress((current) => ({ key: sceneKey, selectedLocationId: id, investigatedIds: current.key === sceneKey ? current.investigatedIds : [] }))
  const addInvestigatedLocation = (id: string) => setSceneProgress((current) => {
    const previous = current.key === sceneKey ? current.investigatedIds : []
    const investigated = previous.includes(id) || previous.length >= requiredInvestigationCount ? previous : [...previous, id]
    return { key: sceneKey, selectedLocationId: id, investigatedIds: investigated }
  })
  const contextualPacts = teamScene?.contextualPacts ?? []
  const activeInteractionId = interactionId || contextualPacts[0]?.interactionTypeId || ''
  const interaction = contextualPacts.find((item) => item.interactionTypeId === activeInteractionId)
  const controlledLocation = descriptor.locations.find((location) => location.controlled)
  const selectedInvestigation = teamScene?.investigationLocations.find((location) => location.id === selectedLocationId)
  const currentCheckpoint = experience.currentCheckpointId ?? world.currentCheckpointId ?? storylet?.checkpointId
  const eventTitle = teamScene?.title ?? ''
  const currentReaction = reactionIndex >= 0 ? descriptor.reactions[reactionIndex] : undefined
  const activeMarkerLocationId = currentReaction?.locationIds[0]
    ?? (storyPhase === 'business' || storyPhase === 'decision' || storyPhase === 'reaction' ? controlledLocation?.id : undefined)
    ?? teamScene?.investigationLocations[0]?.id
  const urgentMarkerLocationIds = currentReaction?.locationIds ?? []
  const markerPresentation = experience.worldPresentation ?? world.worldPresentation
  const markerLocations = (markerPresentation?.locations ?? []).filter((location) => location.presentationTags.includes('team-map')).map((location) => ({ id: location.id, label: location.displayName, x: location.x, y: location.y, businessIdentity: location.businessIdentity }))
  const relevantMarkerIds = new Set<string>(teamScene?.investigationLocations.map((location) => location.id) ?? [])
  const inactiveLocationIds = new Set(descriptor.locations.filter((location) => !location.active).map((location) => location.id))
  const investigationComplete = requiredInvestigationCount > 0 && currentProgress.investigatedIds.length >= requiredInvestigationCount
  const investigationOpen = !storylet?.submitted && (storyPhase === 'investigation' || storyPhase === 'evidence')
  const disabledMarkerLocationIds = markerLocations.filter((marker) => !relevantMarkerIds.has(marker.id) || inactiveLocationIds.has(marker.id) || !investigationOpen || (investigationComplete && !currentProgress.investigatedIds.includes(marker.id))).map((marker) => marker.id)
  const handleMarkerSelect = (locationId?: string) => {
    if (locationId && relevantMarkerIds.has(locationId)) {
      if (!investigationOpen || (investigationComplete && !currentProgress.investigatedIds.includes(locationId))) return
      setProgressSelectedLocation(locationId)
      setStoryPhase(currentProgress.investigatedIds.includes(locationId) ? 'evidence' : 'investigation')
      setSceneSheetOpen(true)
      return
    }
    if (!locationId) {
      setSceneProgress((current) => ({ ...current, key: sceneKey, selectedLocationId: undefined }))
      setSceneSheetOpen(false)
    }
  }
  const businessName = experience.businessPresentation?.displayName ?? experience.controlledEntity?.displayName ?? experience.team.displayName
  const openingParagraphs = teamScene?.openingNarrative ?? []
  const investigatedEvidence = currentProgress.investigatedIds.flatMap((locationId) => teamScene?.investigationLocations.find((location) => location.id === locationId)?.evidence ?? [])
  const businessActivity = teamScene?.businessActivity
  const businessMeaning = businessActivity?.implication ?? experience.businessPresentation?.pulse[0] ?? 'اطلاعات اثر فعالیت در دسترس نیست.'
  const decisionFindings = investigatedEvidence.filter((item) => item.certainty === 'confirmed').map((item) => item.description)
  const investigatedLocations = currentProgress.investigatedIds.map((id) => teamScene?.investigationLocations.find((location) => location.id === id)?.title).filter((title): title is string => Boolean(title))
  const pactUnlocked = contextualPacts.length > 0
  const proposals = [...(experience.inbox ?? []), ...(experience.outbox ?? [])]
  const hasMessages = proposals.length > 0
  const hasPacts = pactUnlocked && investigationComplete && !storylet?.submitted
  const selectedChoiceId = chosenChoiceId ?? storylet?.submittedChoiceId ?? undefined
  const selectedChoice = selectedChoiceId ? storylet?.choices.find((choice) => choice.id === selectedChoiceId) : undefined
  const choiceLabel = selectedChoice?.presentation?.shortTitle ?? selectedChoice?.label ?? 'تصمیم حجره ثبت شد'
  const reactionText = teamScene?.marketReactions[0]?.outcomeLine ?? descriptor.reactions[0]?.outcomeLine ?? descriptor.pulse[0] ?? 'واکنش این تصمیم در دسترس نیست.'
  const additionalReactions = teamScene?.marketReactions.slice(1).map((reaction) => reaction.outcomeLine) ?? []
  const continueToNextScene = () => {
    setIsContinuing(true)
    setNotice('در حال همگام‌سازی صحنه بعد…')
    void client.invalidateQueries({ queryKey: queryKeys.teamExperience })
    void client.invalidateQueries({ queryKey: queryKeys.publicWorld(experience.sessionId) })
  }

  useEffect(() => {
    setStoryPhase(storylet?.submitted ? 'reaction' : 'opening')
    setChosenChoiceId(undefined)
    setIsContinuing(false)
    setSceneSheetOpen(true)
    setReviewedCommunication(false)
  }, [sceneKey])
  useEffect(() => {
    if (teamScene?.marketReactions.length) return
    if (seenReactionVersion === undefined) { markReactionSeen(world.id, descriptor.visualVersion); return }
    if (descriptor.visualVersion <= seenReactionVersion || !descriptor.reactions.length) return
    const timer = window.setTimeout(() => setReactionIndex(0), 0)
    return () => window.clearTimeout(timer)
  }, [descriptor.reactions.length, descriptor.visualVersion, markReactionSeen, seenReactionVersion, teamScene?.marketReactions.length, world.id])

  const choiceMutation = useMutation({
    mutationFn: ({ assignmentId, choiceId }: { assignmentId: string; choiceId: string }) => api.submitChoice(assignmentId, choiceId, experience.stateVersion, token),
    onSuccess: () => {
      setNotice('تصمیم حجره ثبت شد؛ حالا نتیجه را ببینید.')
      setStoryPhase('reaction')
      void client.invalidateQueries({ queryKey: queryKeys.teamExperience })
      void client.invalidateQueries({ queryKey: queryKeys.publicWorld(experience.sessionId) })
    },
    onError: () => setNotice('ثبت تصمیم انجام نشد؛ وضعیت بازار را تازه کنید و دوباره تلاش کنید.'),
  })
  const pactMutation = useMutation({
    mutationFn: async () => {
      if (!interaction || !pactTargetTeamId) throw new Error('برای پیشنهاد پیمان، مقصد و نوع همکاری را انتخاب کنید.')
      const payload = materializeTerms(interaction.termsSchema, terms)
      if (!validatorFor(interaction.termsSchema).safeParse(payload).success) throw new Error('بندهای پیشنهاد را کامل کنید.')
      return api.sendProposal({ interactionTypeId: interaction.interactionTypeId, receiverTeamId: pactTargetTeamId, termsPayload: payload, validityType: 'ValidForCheckpointCount', validForCheckpointCount: validForCheckpoints, validUntilCheckpointId: null, expectedStateVersion: experience.stateVersion, commandId: crypto.randomUUID() }, token)
    },
    onSuccess: () => {
      setNotice('پیام پیمان از مسیر بازار فرستاده شد؛ ردّ نامه در روایت می‌ماند.')
      setTerms({})
      setUtilityPanel('messages')
      void client.invalidateQueries({ queryKey: queryKeys.teamExperience })
      void client.invalidateQueries({ queryKey: queryKeys.publicWorld(experience.sessionId) })
    },
  })
  const investigationMutation = useMutation({
    mutationFn: ({ locationId, evidenceIds }: { locationId: string; evidenceIds: string[] }) => api.recordInvestigation(locationId, evidenceIds, experience.stateVersion, token),
    onSuccess: (_, variables) => {
      addInvestigatedLocation(variables.locationId)
      setSceneProgress((current) => ({ ...current, key: sceneKey, selectedLocationId: undefined }))
      setStoryPhase('investigation')
      setSceneSheetOpen(false)
      setNotice('نشانه‌های این مکان در دفتر حجره ثبت شد.')
      void client.invalidateQueries({ queryKey: queryKeys.teamExperience })
    },
    onError: () => setNotice('ثبت بررسی انجام نشد؛ وضعیت حجره را تازه کنید و دوباره تلاش کنید.'),
  })

  const investigateSelected = () => {
    if (!canWrite || !selectedInvestigation || currentProgress.investigatedIds.includes(selectedInvestigation.id) || investigationComplete || investigationMutation.isPending) return
    investigationMutation.mutate({ locationId: selectedInvestigation.id, evidenceIds: selectedInvestigation.evidence.map((item) => item.id) })
  }

  const remainingInvestigations = Math.max(0, requiredInvestigationCount - currentProgress.investigatedIds.length)
  const primaryAction = (() => {
    if (storylet?.submitted || storyPhase === 'reaction') return { label: 'ادامه به صحنه بعد', onClick: continueToNextScene, disabled: isContinuing }
    if (!investigationComplete) return {
      label: remainingInvestigations === 1 ? 'یک بررسی باقی مانده' : 'انتخاب مکان‌ها',
      onClick: () => { setStoryPhase('investigation'); setSceneSheetOpen(false) },
      disabled: false,
    }
    if (storyPhase === 'investigation') return { label: 'مرور یافته‌ها', onClick: () => { setStoryPhase('evidence'); setSceneSheetOpen(true) }, disabled: false }
    if (storyPhase === 'evidence') return { label: 'ورود به فعالیت کسب‌وکار', onClick: () => { setStoryPhase('business'); setSceneSheetOpen(true) }, disabled: !businessActivity }
    if (storyPhase === 'business' && (hasMessages || hasPacts) && !reviewedCommunication) return { label: 'مرور پیام یا پیمان', onClick: () => { setReviewedCommunication(true); setUtilityPanel(hasMessages ? 'messages' : 'pacts') }, disabled: false }
    if (storyPhase === 'business') return { label: 'مهر تصمیم نهایی', onClick: () => { setStoryPhase('decision'); setSceneSheetOpen(true) }, disabled: false }
    return { label: 'مهر تصمیم نهایی', onClick: () => setSceneSheetOpen(true), disabled: choiceMutation.isPending }
  })()

  if (!teamScene) return <TeamSceneUnavailableState />
  if (!storylet) {
    return <TeamSceneUnavailableState />
  }

  return <section className="hc-play-screen" dir="rtl" data-testid="team-gameplay-screen">
    <header className="hc-play-header">
      <div className="hc-play-identity"><p className="brand">HAMBAFT <span>/ هزارچراغ</span></p><div><strong>{experience.controlledEntity?.displayName ?? experience.team.displayName}</strong><span className="hc-play-divider">|</span><span>{descriptor.atmosphereLabel}</span></div></div>
      <div className="hc-play-scene-title"><span className="hc-play-kicker">{checkpointLabel(currentCheckpoint)}</span><h1>{eventTitle}</h1></div>
      <div className="hc-play-header-actions"><span className={`hc-semantic-chip pressure-${descriptor.pressure}`}>{marketMood(descriptor.pressure)}</span>{hasMessages && <button type="button" className={`hc-utility-button${utilityPanel === 'messages' ? ' is-active' : ''}`} aria-pressed={utilityPanel === 'messages'} onClick={() => setUtilityPanel((current) => current === 'messages' ? null : 'messages')}>پیام‌ها<b>{proposals.length}</b></button>}{hasPacts && <button type="button" className={`hc-utility-button${utilityPanel === 'pacts' ? ' is-active' : ''}`} aria-pressed={utilityPanel === 'pacts'} onClick={() => setUtilityPanel((current) => current === 'pacts' ? null : 'pacts')}>پیمان‌ها{experience.agreements?.length ? <b>{experience.agreements.length}</b> : null}</button>}</div>
    </header>
    {notice && <div className="hc-play-notice" role="status" aria-live="polite">{notice}</div>}
    <div className="hc-play-main">
      <BusinessPanel experience={experience} descriptor={descriptor} activity={businessActivity} actionLabel={primaryAction.label} actionDisabled={primaryAction.disabled} onAction={primaryAction.onClick} />
      <div className="hc-play-stage-column"><div className="hc-play-stage-surface"><EmptyMarketStage markers={markerLocations} ownedLocationId={controlledLocation?.id} activeLocationId={activeMarkerLocationId} selectedLocationId={selectedLocationId} urgentLocationIds={urgentMarkerLocationIds} disabledLocationIds={disabledMarkerLocationIds} investigatedLocationIds={currentProgress.investigatedIds} onSelectLocation={handleMarkerSelect} />
        {sceneSheetOpen && selectedInvestigation && <LocationSceneSheet location={selectedInvestigation} investigated={currentProgress.investigatedIds.includes(selectedInvestigation.id)} pending={investigationMutation.isPending} onConfirm={investigateSelected} onClose={() => handleMarkerSelect()} />}
        {sceneSheetOpen && !selectedInvestigation && storyPhase === 'opening' && <SceneSheet title={eventTitle} subtitle={storylet.presentationTags.speaker} onClose={() => { setStoryPhase('investigation'); setSceneSheetOpen(false) }}><StoryOpeningCard title={eventTitle} speaker={storylet.presentationTags.speaker} paragraphs={openingParagraphs} instruction={teamScene.objective} /></SceneSheet>}
        {sceneSheetOpen && !selectedInvestigation && storyPhase === 'evidence' && <SceneSheet title={teamScene.finalDecisionPresentation?.heading ?? eventTitle} onClose={() => setSceneSheetOpen(false)}><DecisionSummary findings={decisionFindings} investigatedLocations={investigatedLocations} businessMeaning={businessMeaning} presentation={teamScene.finalDecisionPresentation} /></SceneSheet>}
        {sceneSheetOpen && !selectedInvestigation && storyPhase === 'business' && businessActivity && <SceneSheet title={businessActivity.title} subtitle={businessName} onClose={() => setSceneSheetOpen(false)}><BusinessActionCard businessName={businessName} activity={businessActivity} contextualPacts={contextualPacts} onOpenPact={() => setUtilityPanel('pacts')} onContinue={() => setStoryPhase('decision')} showActions={false} /></SceneSheet>}
        {sceneSheetOpen && !selectedInvestigation && storyPhase === 'decision' && <SceneSheet title={teamScene.finalDecisionPresentation?.heading ?? eventTitle} onClose={() => setSceneSheetOpen(false)}><DecisionSummary findings={decisionFindings} investigatedLocations={investigatedLocations} businessMeaning={businessMeaning} presentation={teamScene.finalDecisionPresentation} /><ChoiceCards storylet={storylet} disabled={!canWrite || choiceMutation.isPending} onChoose={(choiceId) => { setChosenChoiceId(choiceId); choiceMutation.mutate({ assignmentId: storylet.assignmentId, choiceId }) }} />{choiceMutation.error && <p className="hc-form-error">{choiceMutation.error.message}</p>}</SceneSheet>}
        {sceneSheetOpen && !selectedInvestigation && storyPhase === 'reaction' && <SceneSheet title={choiceLabel} onClose={() => setSceneSheetOpen(false)}><DecisionSummary findings={decisionFindings} investigatedLocations={investigatedLocations} businessMeaning={businessMeaning} presentation={teamScene.finalDecisionPresentation} /><MarketReactionCard choiceLabel={choiceLabel} reaction={reactionText} additionalReactions={additionalReactions} waiting={!teamScene.marketReactions.length && !descriptor.reactions.length} continuing onContinue={continueToNextScene} /></SceneSheet>}
        {currentReaction && <SceneSheet title={eventTitle} onClose={() => { markReactionSeen(world.id, descriptor.visualVersion); setReactionIndex(-1) }}><WorldReactionStep reaction={currentReaction} index={reactionIndex} total={descriptor.reactions.length} onNext={() => { if (reactionIndex >= descriptor.reactions.length - 1) { markReactionSeen(world.id, descriptor.visualVersion); setReactionIndex(-1) } else setReactionIndex((current) => current + 1) }} onSkip={() => { markReactionSeen(world.id, descriptor.visualVersion); setReactionIndex(-1) }} /></SceneSheet>}
      </div></div>
    </div>
    <SceneBottomStrip descriptor={descriptor} experience={experience} pactUnlocked={hasPacts} onPacts={() => setUtilityPanel('pacts')} />
    {utilityPanel === 'messages' && hasMessages && <MessagesDrawer experience={experience} world={world} onClose={() => setUtilityPanel(null)} />}
    {utilityPanel === 'pacts' && hasPacts && <PactsDrawer pacts={contextualPacts} selectedTeamId={pactTargetTeamId} interactionId={activeInteractionId} terms={terms} validForCheckpoints={validForCheckpoints} mutation={pactMutation} onClose={() => setUtilityPanel(null)} onTargetSelect={(pact) => { setPactTargetTeamId(pact.targetTeamId); setInteractionId(pact.interactionTypeId) }} onInteractionChange={(id) => { setInteractionId(id); setTerms({}) }} onTermsChange={setTerms} onValidityChange={setValidForCheckpoints} />}
  </section>
}

function BusinessPanel({ experience, descriptor, activity, actionLabel, actionDisabled, onAction }: { experience: TeamExperience; descriptor: ReturnType<typeof buildSceneDescriptor>; activity: TeamScenePresentation['businessActivity']; actionLabel: string; actionDisabled: boolean; onAction: () => void }) {
  const business = experience.businessPresentation
  const signals = business?.pulse.slice(0, 3) ?? descriptor.businessPulse.slice(0, 3)
  return <aside className="hc-business-panel" aria-label="وضعیت کسب‌وکار شما"><div className="hc-business-emblem" aria-hidden="true">{business?.businessIdentity ?? 'حجره'}</div><span className="eyebrow">کسب‌وکار شما</span><h2>{business?.displayName ?? experience.controlledEntity?.displayName ?? experience.team.displayName}</h2><strong className="hc-business-condition">{signals[0] ?? activity?.summary ?? 'وضعیت کسب‌وکار هنوز ثبت نشده است.'}</strong><ul>{signals.slice(1).map((signal) => <li key={signal}>{signal}</li>)}{signals.length < 2 && activity?.unlockedActions.slice(0, 1).map((action) => <li key={action}>{action}</li>)}</ul><button type="button" className="button button--gold" disabled={actionDisabled} onClick={onAction}>{actionLabel}</button></aside>
}

function WorldReactionStep({ reaction, index, total, onNext, onSkip }: { reaction: ReturnType<typeof buildSceneDescriptor>['reactions'][number]; index: number; total: number; onNext: () => void; onSkip: () => void }) {
  const last = index >= total - 1
  return <div className="hc-world-reaction" data-testid="world-reaction"><span className="hc-reaction-seal" aria-hidden="true">✦</span><p>{reaction.outcomeLine}</p><small>روایت روی مکان درگیر متمرکز شده و تغییر همان‌جا باقی می‌ماند.</small><div className="button-row"><button type="button" className="button button--gold" onClick={onNext}>{last ? 'ادامه روایت' : 'واکنش بعدی'}</button>{!last && <button type="button" className="button button--ghost" onClick={onSkip}>رد کردن واکنش‌ها</button>}</div></div>
}

function ChoiceCards({ storylet, disabled, onChoose }: { storylet: Storylet; disabled: boolean; onChoose: (choiceId: string) => void }) {
  return <div className="hc-choice-grid" aria-label="انتخاب‌های این پرده">{storylet.choices.map((choice) => { const presentation = choice.presentation; return <button type="button" key={choice.id} className="hc-choice-card" disabled={disabled} onClick={() => onChoose(choice.id)}><span className="hc-choice-title">{presentation?.shortTitle ?? choice.label}</span><span>{presentation?.action ?? choice.shortOutcome ?? 'این انتخاب مسیر حجره را تغییر می‌دهد.'}</span>{presentation?.knownRisk && <small>ریسک: {presentation.knownRisk}</small>}</button> })}</div>
}

function SceneBottomStrip({ descriptor, experience, pactUnlocked, onPacts }: { descriptor: ReturnType<typeof buildSceneDescriptor>; experience: TeamExperience; pactUnlocked: boolean; onPacts: () => void }) {
  const proposals = [...(experience.inbox ?? []), ...(experience.outbox ?? [])].filter((proposal, index, all) => all.findIndex((item) => item.proposalId === proposal.proposalId) === index).slice(0, 2)
  const agreements = experience.agreements ?? []
  const semanticState = experience.worldPresentation?.semanticMetrics ?? []
  return <footer className="hc-scene-footer"><div className="hc-footer-block"><span className="eyebrow">سیگنال‌های اخیر</span><p>{descriptor.pulse[0] || descriptor.publicEvent || 'هنوز سیگنال تازه‌ای ثبت نشده است.'}</p><small>{descriptor.businessPulse[0] ?? 'وضعیت کسب‌وکار در روایت این صحنه دنبال می‌شود.'}</small></div>{(agreements.length > 0 || pactUnlocked) && <div className="hc-footer-block hc-footer-pacts"><span className="eyebrow">پیمان‌های مرتبط</span>{agreements.length ? <div className="hc-footer-items">{agreements.slice(0, 2).map((agreement) => <span key={agreement.agreementId}>{agreementStatusLabel(agreement.status)} · پیمان بازار</span>)}</div> : <button type="button" className="hc-inline-action" onClick={onPacts}>دیدن پیمان‌های مرتبط</button>}</div>}{proposals.length > 0 && <div className="hc-footer-block"><span className="eyebrow">پیام‌های مؤثر</span><div className="hc-footer-items">{proposals.map((proposal) => <span key={proposal.proposalId}>{proposalStatusLabel(proposal.status)} · نامه‌ی بازار</span>)}</div></div>}<div className="hc-footer-block hc-footer-state"><span className="eyebrow">حالت حجره و جهان</span><strong>{marketMood(descriptor.pressure)}</strong><small>{semanticState[0]?.description ?? 'اعتماد و فشار از رفتار بازار خوانده می‌شود، نه از عدد.'}</small></div></footer>
}

function MessagesDrawer({ experience, world, onClose }: { experience: TeamExperience; world: PublicWorld; onClose: () => void }) {
  const proposals = [...(experience.inbox ?? []), ...(experience.outbox ?? [])].filter((proposal, index, all) => all.findIndex((item) => item.proposalId === proposal.proposalId) === index).slice(0, 4)
  const teamName = (teamId: string) => world.entities.find((entity) => entity.controlledByTeamId === teamId)?.displayName ?? 'حجره‌ی دیگر'
  return <aside className="hc-utility-drawer" aria-label="پیام‌های بازار"><header><div><span className="eyebrow">ارتباط در دل بازار</span><h2>پیام‌های مؤثر بر تصمیم</h2></div><button type="button" className="panel-close" onClick={onClose} aria-label="بستن پیام‌ها">×</button></header><div className="hc-drawer-list">{proposals.map((proposal) => <article className="hc-mini-letter" key={proposal.proposalId}><div><strong>{teamName(proposal.senderTeamId)} ← {teamName(proposal.receiverTeamId)}</strong><span>{proposalStatusLabel(proposal.status)}</span></div><p>{proposal.contextualMessage ?? 'متن زمینه‌ای این نامه در دسترس نیست.'}</p></article>)}</div></aside>
}

function PactsDrawer({ pacts, selectedTeamId, interactionId, terms, validForCheckpoints, mutation, onClose, onTargetSelect, onInteractionChange, onTermsChange, onValidityChange }: { pacts: ContextualPactPresentation[]; selectedTeamId: string; interactionId: string; terms: TermDraft; validForCheckpoints: number; mutation: UseMutationResult<unknown, Error, void, unknown>; onClose: () => void; onTargetSelect: (pact: ContextualPactPresentation) => void; onInteractionChange: (id: string) => void; onTermsChange: (terms: TermDraft) => void; onValidityChange: (count: number) => void }) {
  const target = pacts.find((pact) => pact.targetTeamId === selectedTeamId)
  const targetPacts = target ? pacts.filter((pact) => pact.targetTeamId === selectedTeamId) : []
  const interaction = targetPacts.find((pact) => pact.interactionTypeId === interactionId) ?? targetPacts[0]
  const targets = pacts.filter((pact, index, all) => all.findIndex((candidate) => candidate.targetTeamId === pact.targetTeamId) === index)
  return <aside className="hc-utility-drawer hc-pacts-drawer" aria-label="پیمان‌های بازار"><header><div><span className="eyebrow">همکاری میان حجره‌ها</span><h2>{target ? `نامه برای ${target.targetDisplayName}` : 'پیمان‌های بازار'}</h2></div><button type="button" className="panel-close" onClick={onClose} aria-label="بستن پیمان‌ها">×</button></header>{!target ? <><p className="hc-drawer-intro">مقصد را از فهرست پیشنهادهای این صحنه انتخاب کنید.</p><div className="hc-target-list">{targets.map((pact) => <button type="button" key={pact.targetTeamId} className="hc-target-row" onClick={() => onTargetSelect(pact)}><strong>{pact.targetDisplayName}</strong><span>{pact.title}</span><small>{pact.unlock} · ریسک: {pact.risk}</small></button>)}</div></> : <><div className="hc-pact-target"><span className="eyebrow">مقصد انتخاب‌شده</span><strong>{target.targetDisplayName}</strong><small>{target.unlock}</small><p>{target.risk}</p></div>{targetPacts.length > 1 && <label className="hc-drawer-field"><span>نوع نامه</span><select value={interaction?.interactionTypeId ?? ''} onChange={(event) => onInteractionChange(event.target.value)}>{targetPacts.map((pact) => <option key={pact.id} value={pact.interactionTypeId}>{pact.title}</option>)}</select></label>}{interaction && <div className="hc-proposal-letter"><span className="hc-letter-seal" aria-hidden="true">✦</span><p className="hc-drawer-intro">{interaction.message ?? interaction.description}</p><TermsEditor schema={interaction.termsSchema} value={terms} onChange={onTermsChange} /><label className="hc-drawer-field"><span>اعتبار نامه</span><select value={validForCheckpoints} onChange={(event) => onValidityChange(Number(event.target.value))}><option value={1}>تا پرده‌ی بعد</option><option value={2}>تا دو پرده‌ی بعد</option></select></label><small className="hc-known-consequence">اثر شناخته‌شده: پس از پذیرش، تعهد میان طرف‌ها ثبت می‌شود.</small></div>}<button type="button" className="button button--gold hc-send-pact" disabled={mutation.isPending || !interaction} onClick={() => mutation.mutate(undefined)}>{mutation.isPending ? 'در حال مهر و ارسال…' : 'مهر و ارسال نامه'}</button>{mutation.isError && <p className="hc-form-error">{mutation.error instanceof Error ? mutation.error.message : 'ارسال پیشنهاد انجام نشد.'}</p>}</>}</aside>
}

function marketMood(pressure: string) {
  return ({ calm: 'اعتماد بازار برقرار است', uneasy: 'فشار بازار رو به افزایش است', strained: 'انسجام بازار شکننده است', critical: 'بازار در وضعیت بحرانی است' } as Record<string, string>)[pressure] ?? 'بازار در حال تغییر است'
}

function validatorFor(schema: TermSchema): z.ZodType {
  if (['Numeric', 'NumericRange'].includes(schema.type)) return z.number().min(schema.minimum ?? -Infinity).max(schema.maximum ?? Infinity)
  if (schema.type === 'Boolean') return z.boolean()
  if (['ShortText', 'ShortDeclaration', 'SingleChoice', 'TargetSelection', 'DocumentSelection', 'EvidenceSelection'].includes(schema.type)) return (schema.required ? z.string().min(1) : z.string()).max(schema.maximumLength ?? Infinity)
  if (['MultipleChoice', 'RankedChoice'].includes(schema.type)) return z.array(z.string())
  if (schema.type === 'NumericAllocation') return z.record(z.string(), z.number())
  const fields: Record<string, z.ZodType> = {}
  schema.fields.forEach((field) => { if (field.name) fields[field.name] = field.required ? validatorFor(field) : validatorFor(field).optional() })
  return z.object(fields)
}

function materializeTerms(schema: TermSchema, draft: TermDraft): TermDraft {
  const result = { ...draft }
  const fields = schema.type === 'Compound' ? schema.fields : [schema]
  fields.forEach((field) => { if (field.type === 'Boolean' && field.name && result[field.name] === undefined) result[field.name] = false })
  return result
}
