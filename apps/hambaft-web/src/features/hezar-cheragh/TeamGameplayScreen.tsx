import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { z } from 'zod'
import { api } from '../../api/client'
import { queryKeys } from '../../api/queryKeys'
import type { PublicWorld, StoryPackage, TeamExperience, TermSchema } from '../../api/schemas'
import { useAuthStore } from '../../auth/authStore'
import { usePackage } from '../../hooks/useServerQuery'
import { agreementStatusLabel, checkpointLabel, proposalStatusLabel } from '../../design-system/presentation'
import { TermsEditor, type TermDraft } from '../../components/TermsView'
import { EmptyMarketStage } from './EmptyMarketStage'
import { TEAM_MARKET_MARKERS } from './TeamMarketMarkers'
import { BusinessActionCard, DecisionSummary, EvidenceReveal, InvestigationPrompt, LocationInvestigationCard, MarketReactionCard, StoryOpeningCard, type EvidenceItem, type TeamStoryPhase } from './TeamStoryPresentation'
import type { SceneLocation } from '../visual-world/types'
import { buildSceneDescriptor } from '../visual-world/sceneDirector'
import { useUiStore } from '../../state/uiStore'

type Storylet = TeamExperience['privateStorylets'][number]
type UtilityPanel = 'messages' | 'pacts' | null
const REQUIRED_INVESTIGATIONS = 2
const INVESTIGATION_LOCATION_IDS = ['haj-sadegh-office', 'market-entrance', 'logistics-rah-no'] as const

export function TeamGameplayScreen({ experience, world, canWrite }: { experience: TeamExperience; world: PublicWorld; canWrite: boolean }) {
  const token = useAuthStore((state) => state.team?.accessToken ?? '')
  const client = useQueryClient()
  const descriptor = useMemo(() => buildSceneDescriptor(world, experience), [experience, world])
  const storylet = experience.privateStorylets.find((item) => !item.submitted) ?? experience.privateStorylets[0]
  const sceneKey = `${world.id}:${experience.currentCheckpointId ?? world.currentCheckpointId ?? storylet?.checkpointId ?? 'waiting'}`
  const [sceneProgress, setSceneProgress] = useState<{ key: string; selectedLocationId?: string; investigatedIds: string[] }>({ key: '', investigatedIds: [] })
  const [utilityPanel, setUtilityPanel] = useState<UtilityPanel>(null)
  const [pactTargetTeamId, setPactTargetTeamId] = useState('')
  const [interactionId, setInteractionId] = useState('')
  const [terms, setTerms] = useState<TermDraft>({})
  const [validForCheckpoints, setValidForCheckpoints] = useState(1)
  const [storyPhase, setStoryPhase] = useState<TeamStoryPhase>(storylet?.submitted ? 'reaction' : 'opening')
  const [chosenChoiceId, setChosenChoiceId] = useState<string>()
  const [reactionIndex, setReactionIndex] = useState(-1)
  const [sheetState, setSheetState] = useState<'collapsed' | 'half' | 'expanded'>('half')
  const [notice, setNotice] = useState('')
  const seenReactionVersion = useUiStore((state) => state.seenReactionVersions[world.id])
  const markReactionSeen = useUiStore((state) => state.markReactionSeen)

  const availableInteractions = useMemo(() => experience.availableInteractionTypes ?? [], [experience.availableInteractionTypes])
  const currentProgress = sceneProgress.key === sceneKey ? sceneProgress : { key: sceneKey, investigatedIds: [] }
  const selectedLocationId = currentProgress.selectedLocationId
  const investigatedIds = currentProgress.investigatedIds
  const setProgressSelectedLocation = (id: string) => setSceneProgress((current) => ({ key: sceneKey, selectedLocationId: id, investigatedIds: current.key === sceneKey ? current.investigatedIds : [] }))
  const addInvestigatedLocation = (id: string) => setSceneProgress((current) => {
    const previous = current.key === sceneKey ? current.investigatedIds : []
    const investigated = previous.includes(id) || previous.length >= REQUIRED_INVESTIGATIONS ? previous : [...previous, id]
    return { key: sceneKey, selectedLocationId: id, investigatedIds: investigated }
  })
  const packageQuery = usePackage(experience.sessionMetadata?.storyPackageId, experience.sessionMetadata?.storyVersion)
  const activeInteractionId = interactionId || availableInteractions[0]?.interactionTypeId || ''
  const interaction = packageQuery.data?.interactions.find((item) => item.id === activeInteractionId)
  const allowedTargetIds = useMemo(() => new Set(availableInteractions.flatMap((item) => item.allowedTargetTeamIds).filter((id) => id !== experience.team.id)), [availableInteractions, experience.team.id])
  const targetLocations = useMemo(() => descriptor.locations.filter((location) => Boolean(location.teamId && allowedTargetIds.has(location.teamId))), [allowedTargetIds, descriptor.locations])
  const controlledLocation = descriptor.locations.find((location) => location.controlled)
  const selectedLocation = descriptor.locations.find((location) => location.id === selectedLocationId) ?? presentationLocationFor(selectedLocationId)
  const currentCheckpoint = experience.currentCheckpointId ?? world.currentCheckpointId ?? storylet?.checkpointId
  const eventTitle = playerFacingStoryTitle(storylet?.title, currentCheckpoint, descriptor.atmosphereLabel)
  const currentReaction = reactionIndex >= 0 ? descriptor.reactions[reactionIndex] : undefined
  const activeMarkerLocationId = currentReaction?.locationIds[0]
    ?? (storyPhase === 'business' || storyPhase === 'decision' || storyPhase === 'reaction' ? controlledLocation?.id : undefined)
    ?? (currentCheckpoint === 'morning-without-bell' ? 'haj-sadegh-office' : undefined)
  const urgentMarkerLocationIds = currentReaction?.locationIds ?? []
  const relevantMarkerIds = new Set<string>(INVESTIGATION_LOCATION_IDS)
  const inactiveLocationIds = new Set(descriptor.locations.filter((location) => !location.active).map((location) => location.id))
  const investigationComplete = investigatedIds.length >= REQUIRED_INVESTIGATIONS
  const investigationOpen = storyPhase === 'investigation' || storyPhase === 'evidence'
  const disabledMarkerLocationIds = TEAM_MARKET_MARKERS.filter((marker) => !relevantMarkerIds.has(marker.id) || inactiveLocationIds.has(marker.id) || !investigationOpen || (investigationComplete && !investigatedIds.includes(marker.id))).map((marker) => marker.id)
  const handleMarkerSelect = (locationId?: string) => {
    if (locationId && relevantMarkerIds.has(locationId)) {
      if (!investigationOpen) return
      if (investigationComplete && !investigatedIds.includes(locationId)) return
      setProgressSelectedLocation(locationId)
      setStoryPhase(investigatedIds.includes(locationId) ? 'evidence' : 'investigation')
      return
    }
    if (!locationId) {
      setSceneProgress((current) => ({ ...current, key: sceneKey, selectedLocationId: undefined }))
      setStoryPhase('opening')
    }
  }
  const businessName = experience.businessPresentation?.displayName ?? experience.controlledEntity?.displayName ?? experience.team.displayName
  const openingParagraphs = openingStoryFor(businessName)
  const evidence = evidenceFor(selectedLocation)
  const investigatedEvidence = investigatedIds.flatMap((locationId) => evidenceFor(descriptor.locations.find((location) => location.id === locationId) ?? presentationLocationFor(locationId)))
  const unlockedActions = [...new Set(investigatedEvidence.flatMap((item) => item.unlocks ?? []))].slice(0, 3)
  const businessSummary = `دو بررسی شما نشان می‌دهد خروج بارها از مسیر اصلی اتفاقی نبوده است. حالا ${businessName} می‌تواند پیش از ظهر بر اساس مدرک تصمیم بگیرد، نه شایعه.`
  const businessMeaning = decisionMeaningFor(investigatedIds, businessName)
  const decisionFindings = investigatedIds.map((locationId) => evidenceFor(descriptor.locations.find((location) => location.id === locationId) ?? presentationLocationFor(locationId)).find((item) => item.certainty === 'confirmed')?.description).filter((finding): finding is string => Boolean(finding))
  const pactUnlocked = investigatedIds.includes('logistics-rah-no') && availableInteractions.length > 0 && targetLocations.length > 0
  const proposals = [...(experience.inbox ?? []), ...(experience.outbox ?? [])]
  const hasMessages = proposals.length > 0
  const hasPacts = pactUnlocked
  const selectedChoiceId = chosenChoiceId ?? storylet?.submittedChoiceId ?? undefined
  const selectedChoice = selectedChoiceId ? storylet?.choices.find((choice) => choice.id === selectedChoiceId) : undefined
  const choiceLabel = selectedChoice?.presentation?.shortTitle ?? selectedChoice?.label ?? 'تصمیم حجره ثبت شد'
  const reactionText = descriptor.reactions[0]?.outcomeLine ?? descriptor.pulse[0] ?? 'خبر تصمیم شما در بازار پیچیده است؛ نتیجه کامل با ادامه این پرده روشن می‌شود.'
  const continueToNextScene = () => {
    setNotice('در حال دریافت ادامه روایت بازار…')
    void client.invalidateQueries({ queryKey: queryKeys.teamExperience })
    void client.invalidateQueries({ queryKey: queryKeys.publicWorld(experience.sessionId) })
  }
  useEffect(() => {
    setStoryPhase(storylet?.submitted ? 'reaction' : 'opening')
    setChosenChoiceId(undefined)
  }, [sceneKey])
  useEffect(() => {
    if (seenReactionVersion === undefined) { markReactionSeen(world.id, descriptor.visualVersion); return }
    if (descriptor.visualVersion <= seenReactionVersion || !descriptor.reactions.length) return
    const timer = window.setTimeout(() => setReactionIndex(0), 0)
    return () => window.clearTimeout(timer)
  }, [descriptor.reactions.length, descriptor.visualVersion, markReactionSeen, seenReactionVersion, world.id])
  const choiceMutation = useMutation({
    mutationFn: ({ assignmentId, choiceId }: { assignmentId: string; choiceId: string }) => api.submitChoice(assignmentId, choiceId, experience.stateVersion, token),
    onSuccess: () => {
      setNotice('تصمیم حجره ثبت شد؛ حالا واکنش بازار را ببینید.')
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
      return api.sendProposal({
        interactionTypeId: interaction.id,
        receiverTeamId: pactTargetTeamId,
        termsPayload: payload,
        validityType: 'ValidForCheckpointCount',
        validForCheckpointCount: validForCheckpoints,
        validUntilCheckpointId: null,
        expectedStateVersion: experience.stateVersion,
        commandId: crypto.randomUUID(),
      }, token)
    },
    onSuccess: () => {
      setNotice('پیام پیمان از مسیر بازار فرستاده شد؛ ردّ نامه در روایت می‌ماند.')
      setTerms({})
      setUtilityPanel('messages')
      void client.invalidateQueries({ queryKey: queryKeys.teamExperience })
      void client.invalidateQueries({ queryKey: queryKeys.publicWorld(experience.sessionId) })
    },
  })

  const investigateSelected = () => {
    if (!selectedLocation || investigatedIds.includes(selectedLocation.id) || investigationComplete) return
    addInvestigatedLocation(selectedLocation.id)
    setStoryPhase('evidence')
    setNotice(`نشانه‌ی ${selectedLocation.name} در دفتر حجره ثبت شد.`)
  }

  if (!storylet) {
    return <section className="hc-play-screen hc-play-screen--empty" dir="rtl"><div className="hc-empty-card"><span className="eyebrow">{checkpointLabel(currentCheckpoint)}</span><h1>{experience.entityEnding ? experience.entityEnding.title : 'بازار در سکوتِ بین دو پرده'}</h1><p>{experience.entityEnding?.paragraphs[0] ?? 'پرده‌ی بعدی هنوز برای این حجره باز نشده است.'}</p><p className="form-help">وضعیت حجره در همین صفحه نگه داشته می‌شود.</p></div></section>
  }

  return <section className="hc-play-screen" dir="rtl" data-testid="team-gameplay-screen">
    <header className="hc-play-header">
      <div className="hc-play-identity">
        <p className="brand">HAMBAFT <span>/ هزارچراغ</span></p>
        <div><strong>{experience.controlledEntity?.displayName ?? experience.team.displayName}</strong><span className="hc-play-divider">|</span><span>{descriptor.atmosphereLabel}</span></div>
      </div>
      <div className="hc-play-scene-title"><span className="hc-play-kicker">{checkpointLabel(currentCheckpoint)}</span><h1>{eventTitle}</h1></div>
      <div className="hc-play-header-actions">
        <span className={`hc-semantic-chip pressure-${descriptor.pressure}`}>{marketMood(descriptor.pressure)}</span>
        {hasMessages && <button type="button" className={`hc-utility-button${utilityPanel === 'messages' ? ' is-active' : ''}`} aria-pressed={utilityPanel === 'messages'} onClick={() => setUtilityPanel((current) => current === 'messages' ? null : 'messages')}>پیام‌ها<b>{proposals.length}</b></button>}
        {hasPacts && <button type="button" className={`hc-utility-button${utilityPanel === 'pacts' ? ' is-active' : ''}`} aria-pressed={utilityPanel === 'pacts'} onClick={() => setUtilityPanel((current) => current === 'pacts' ? null : 'pacts')}>پیمان‌ها{experience.agreements?.length ? <b>{experience.agreements.length}</b> : null}</button>}
      </div>
    </header>

    {notice && <div className="hc-play-notice" role="status" aria-live="polite">{notice}</div>}
    <div className="hc-play-main">
      <BusinessPanel experience={experience} descriptor={descriptor} investigatedCount={investigatedIds.length} canOpen={investigationComplete} onOpen={() => { setStoryPhase('business'); if (controlledLocation) setProgressSelectedLocation(controlledLocation.id) }} />
      <div className="hc-play-stage-column">
        <div className="hc-play-stage-surface"><EmptyMarketStage ownedLocationId={controlledLocation?.id} activeLocationId={activeMarkerLocationId} selectedLocationId={selectedLocationId} urgentLocationIds={urgentMarkerLocationIds} disabledLocationIds={disabledMarkerLocationIds} onSelectLocation={handleMarkerSelect} /></div>
      </div>

      <aside className={`hc-play-panel sheet-${sheetState}`} aria-label="روایت و اقدام پرده" data-mode={currentReaction ? 'WorldReaction' : storyPhase}>
        <div className="hc-sheet-controls" role="group" aria-label="اندازه پنل بازی">{(['collapsed', 'half', 'expanded'] as const).map((state) => <button key={state} type="button" className={sheetState === state ? 'is-active' : ''} aria-pressed={sheetState === state} onClick={() => setSheetState(state)}>{state === 'collapsed' ? 'جمع' : state === 'half' ? 'نیمه' : 'باز'}</button>)}</div>
        <div className="hc-panel-scroll-safe">
          <div className="hc-active-action">
            {currentReaction && <WorldReactionStep reaction={currentReaction} index={reactionIndex} total={descriptor.reactions.length} onNext={() => { if (reactionIndex >= descriptor.reactions.length - 1) { markReactionSeen(world.id, descriptor.visualVersion); setReactionIndex(-1) } else setReactionIndex((current) => current + 1) }} onSkip={() => { markReactionSeen(world.id, descriptor.visualVersion); setReactionIndex(-1) }} />}
            {!currentReaction && storyPhase === 'opening' && <StoryOpeningCard title={eventTitle} speaker="رحیم، شاگرد حجره" paragraphs={openingParagraphs} instruction="دو مکان را بررسی کنید و مشخص کنید بارها چرا از مسیر اصلی خارج شده‌اند." onContinue={() => { setSceneProgress((current) => ({ ...current, key: sceneKey, selectedLocationId: undefined })); setStoryPhase('investigation') }} />}
            {!currentReaction && storyPhase === 'investigation' && !selectedLocation && <InvestigationPrompt investigatedCount={investigatedIds.length} requiredCount={REQUIRED_INVESTIGATIONS} />}
            {!currentReaction && storyPhase === 'investigation' && selectedLocation && <LocationInvestigationCard title={selectedLocation.name} identity={selectedLocation.shortIdentity} narrative={locationNarrativeFor(selectedLocation)} investigatedCount={investigatedIds.length} requiredCount={REQUIRED_INVESTIGATIONS} onInvestigate={investigateSelected} />}
            {!currentReaction && storyPhase === 'evidence' && selectedLocation && <EvidenceReveal locationName={selectedLocation.name} evidence={evidence} investigatedCount={investigatedIds.length} requiredCount={REQUIRED_INVESTIGATIONS} onContinue={() => { if (investigationComplete) { setStoryPhase('business'); if (controlledLocation) setProgressSelectedLocation(controlledLocation.id) } else { setSceneProgress((current) => ({ ...current, key: sceneKey, selectedLocationId: undefined })); setStoryPhase('investigation') } }} />}
            {!currentReaction && storyPhase === 'business' && <BusinessActionCard businessName={businessName} summary={businessSummary} implication={businessMeaning} unlockedActions={unlockedActions} canOfferPact={pactUnlocked} onOpenPact={() => setUtilityPanel('pacts')} onContinue={() => setStoryPhase('decision')} />}
            {!currentReaction && storyPhase === 'decision' && <><DecisionSummary findings={decisionFindings} businessMeaning={businessMeaning} /><ChoiceCards storylet={storylet} disabled={!canWrite || choiceMutation.isPending} onChoose={(choiceId) => { setChosenChoiceId(choiceId); choiceMutation.mutate({ assignmentId: storylet.assignmentId, choiceId }) }} /></>}
            {!currentReaction && storyPhase === 'reaction' && <MarketReactionCard choiceLabel={choiceLabel} reaction={reactionText} waiting={!descriptor.reactions.length} onContinue={continueToNextScene} />}
            {choiceMutation.error && <p className="hc-form-error">{choiceMutation.error.message}</p>}
          </div>
        </div>
      </aside>
    </div>

    <SceneBottomStrip descriptor={descriptor} experience={experience} pactUnlocked={pactUnlocked} onPacts={() => setUtilityPanel('pacts')} />
    {utilityPanel === 'messages' && hasMessages && <MessagesDrawer experience={experience} world={world} onClose={() => setUtilityPanel(null)} />}
    {utilityPanel === 'pacts' && hasPacts && <PactsDrawer experience={experience} packageData={packageQuery.data} targetLocations={targetLocations} selectedTeamId={pactTargetTeamId} interactionId={activeInteractionId} terms={terms} validForCheckpoints={validForCheckpoints} mutation={pactMutation} onClose={() => setUtilityPanel(null)} onTargetSelect={(location) => { setProgressSelectedLocation(location.id); setPactTargetTeamId(location.teamId ?? '') }} onInteractionChange={(id) => { setInteractionId(id); setTerms({}) }} onTermsChange={setTerms} onValidityChange={setValidForCheckpoints} />}
  </section>
}

function BusinessPanel({ experience, descriptor, investigatedCount, canOpen, onOpen }: { experience: TeamExperience; descriptor: ReturnType<typeof buildSceneDescriptor>; investigatedCount: number; canOpen: boolean; onOpen: () => void }) {
  const business = experience.businessPresentation
  const signals = business?.pulse.slice(0, 3) ?? descriptor.businessPulse.slice(0, 3)
  return <aside className="hc-business-panel" aria-label="وضعیت کسب‌وکار شما">
    <div className="hc-business-emblem" aria-hidden="true">{businessIcon(business?.entityDefinitionId ?? experience.controlledEntity?.definitionId)}</div>
    <span className="eyebrow">کسب‌وکار شما</span>
    <h2>{business?.displayName ?? experience.controlledEntity?.displayName ?? experience.team.displayName}</h2>
    <strong className="hc-business-condition">{signals[0] ?? 'آرد سفارش امروز هنوز به حجره نرسیده است.'}</strong>
    <ul>{signals.slice(1).map((signal) => <li key={signal}>{signal}</li>)}{signals.length < 2 && <li>بدون دو مدرک مستقل، فعالیت و تصمیم نهایی باز نمی‌شود.</li>}</ul>
    <button type="button" className="button button--gold" disabled={!canOpen} onClick={onOpen}>{canOpen ? 'ورود به فعالیت کسب‌وکار' : `فعالیت پس از دو بررسی (${investigatedCount}/۲)`}</button>
  </aside>
}

function WorldReactionStep({ reaction, index, total, onNext, onSkip }: { reaction: ReturnType<typeof buildSceneDescriptor>['reactions'][number]; index: number; total: number; onNext: () => void; onSkip: () => void }) {
  const last = index >= total - 1
  return <div className="hc-world-reaction" data-testid="world-reaction">
    <span className="hc-reaction-seal" aria-hidden="true">✦</span>
    <p>{reaction.outcomeLine}</p>
    <small>روایت روی مکان درگیر متمرکز شده و تغییر همان‌جا باقی می‌ماند.</small>
    <div className="button-row"><button type="button" className="button button--gold" onClick={onNext}>{last ? 'ادامه روایت' : 'واکنش بعدی'}</button>{!last && <button type="button" className="button button--ghost" onClick={onSkip}>رد کردن واکنش‌ها</button>}</div>
  </div>
}

function ChoiceCards({ storylet, disabled, onChoose }: { storylet: Storylet; disabled: boolean; onChoose: (choiceId: string) => void }) {
  return <div className="hc-choice-grid" aria-label="انتخاب‌های این پرده">{storylet.choices.map((choice) => { const presentation = choice.presentation; return <button type="button" key={choice.id} className="hc-choice-card" disabled={disabled} onClick={() => onChoose(choice.id)}><span className="hc-choice-title">{presentation?.shortTitle ?? choice.label}</span><span>{presentation?.action ?? choice.shortOutcome ?? 'این انتخاب مسیر حجره را تغییر می‌دهد.'}</span>{presentation?.knownRisk && <small>ریسک: {presentation.knownRisk}</small>}</button> })}</div>
}

function SceneBottomStrip({ descriptor, experience, pactUnlocked, onPacts }: { descriptor: ReturnType<typeof buildSceneDescriptor>; experience: TeamExperience; pactUnlocked: boolean; onPacts: () => void }) {
  const proposals = [...(experience.inbox ?? []), ...(experience.outbox ?? [])].filter((proposal, index, all) => all.findIndex((item) => item.proposalId === proposal.proposalId) === index).slice(0, 2)
  const agreements = experience.agreements ?? []
  const semanticState = experience.worldPresentation?.semanticMetrics ?? []
  return <footer className="hc-scene-footer"><div className="hc-footer-block"><span className="eyebrow">سیگنال‌های اخیر</span><p>{descriptor.pulse[0] || descriptor.publicEvent || 'بارهای صبح از مسیر همیشگی نرسیده‌اند.'}</p><small>{descriptor.businessPulse[0] ?? 'مسیر بار پیش از ظهر روی تصمیم حجره اثر می‌گذارد.'}</small></div>{(agreements.length > 0 || pactUnlocked) && <div className="hc-footer-block hc-footer-pacts"><span className="eyebrow">پیمان‌های مرتبط</span>{agreements.length ? <div className="hc-footer-items">{agreements.slice(0, 2).map((agreement) => <span key={agreement.agreementId}>{agreementStatusLabel(agreement.status)} · پیمان بازار</span>)}</div> : <button type="button" className="hc-inline-action" onClick={onPacts}>تطبیق رسید با باربری</button>}</div>}{proposals.length > 0 && <div className="hc-footer-block"><span className="eyebrow">پیام‌های مؤثر</span><div className="hc-footer-items">{proposals.map((proposal) => <span key={proposal.proposalId}>{proposalStatusLabel(proposal.status)} · نامه‌ی بازار</span>)}</div></div>}<div className="hc-footer-block hc-footer-state"><span className="eyebrow">حالت حجره و جهان</span><strong>{marketMood(descriptor.pressure)}</strong><small>{semanticState[0]?.description ?? 'اعتماد و فشار از رفتار بازار خوانده می‌شود، نه از عدد.'}</small></div></footer>
}

function MessagesDrawer({ experience, world, onClose }: { experience: TeamExperience; world: PublicWorld; onClose: () => void }) {
  const proposals = [...(experience.inbox ?? []), ...(experience.outbox ?? [])].filter((proposal, index, all) => all.findIndex((item) => item.proposalId === proposal.proposalId) === index).slice(0, 4)
  const teamName = (teamId: string) => world.entities.find((entity) => entity.controlledByTeamId === teamId)?.displayName ?? 'حجره‌ی دیگر'
  return <aside className="hc-utility-drawer" aria-label="پیام‌های بازار"><header><div><span className="eyebrow">ارتباط در دل بازار</span><h2>پیام‌های مؤثر بر تصمیم</h2></div><button type="button" className="panel-close" onClick={onClose} aria-label="بستن پیام‌ها">×</button></header><div className="hc-drawer-list">{proposals.map((proposal) => <article className="hc-mini-letter" key={proposal.proposalId}><div><strong>{teamName(proposal.senderTeamId)} ← {teamName(proposal.receiverTeamId)}</strong><span>{proposalStatusLabel(proposal.status)}</span></div><p>این نامه می‌تواند امکان همکاری یا هزینه تصمیم نهایی را تغییر دهد.</p></article>)}</div></aside>
}

function PactsDrawer({ experience, packageData, targetLocations, selectedTeamId, interactionId, terms, validForCheckpoints, mutation, onClose, onTargetSelect, onInteractionChange, onTermsChange, onValidityChange }: { experience: TeamExperience; packageData?: StoryPackage; targetLocations: SceneLocation[]; selectedTeamId: string; interactionId: string; terms: TermDraft; validForCheckpoints: number; mutation: UseMutationResult<unknown, Error, void, unknown>; onClose: () => void; onTargetSelect: (location: SceneLocation) => void; onInteractionChange: (id: string) => void; onTermsChange: (terms: TermDraft) => void; onValidityChange: (count: number) => void }) {
  const interaction = packageData?.interactions.find((item) => item.id === interactionId)
  const target = targetLocations.find((location) => location.teamId === selectedTeamId)
  return <aside className="hc-utility-drawer hc-pacts-drawer" aria-label="پیمان‌های بازار"><header><div><span className="eyebrow">همکاری میان حجره‌ها</span><h2>{target ? `نامه برای ${target.name}` : 'پیمان‌های بازار'}</h2></div><button type="button" className="panel-close" onClick={onClose} aria-label="بستن پیمان‌ها">×</button></header>{!target ? <><p className="hc-drawer-intro">مقصد را از فهرست حجره‌های بازار انتخاب کنید؛ پس از انتخاب، نامه‌ی همکاری آماده می‌شود.</p><div className="hc-target-list">{targetLocations.map((location) => <button type="button" key={location.id} className="hc-target-row" onClick={() => onTargetSelect(location)}><strong>{location.name}</strong><span>{location.currentCondition}</span><small>{location.whyItMatters}</small></button>)}</div></> : <><div className="hc-pact-target"><span className="eyebrow">مقصد انتخاب‌شده</span><strong>{target.name}</strong><small>{target.currentCondition}</small><p>{target.whyItMatters}</p></div>{experience.availableInteractionTypes && experience.availableInteractionTypes.length > 1 && <label className="hc-drawer-field"><span>نوع نامه</span><select value={interactionId} onChange={(event) => onInteractionChange(event.target.value)}>{experience.availableInteractionTypes.map((available) => <option key={available.interactionTypeId} value={available.interactionTypeId}>{packageData?.interactions.find((item) => item.id === available.interactionTypeId)?.displayName ?? 'همکاری بازار'}</option>)}</select></label>}{interaction && <div className="hc-proposal-letter"><span className="hc-letter-seal" aria-hidden="true">✦</span><p className="hc-drawer-intro">{interaction.description}</p><TermsEditor schema={interaction.termsSchema} value={terms} onChange={onTermsChange} /><label className="hc-drawer-field"><span>اعتبار نامه</span><select value={validForCheckpoints} onChange={(event) => onValidityChange(Number(event.target.value))}><option value={1}>تا پرده‌ی بعد</option><option value={2}>تا دو پرده‌ی بعد</option></select></label><small className="hc-known-consequence">اثر شناخته‌شده: پس از پذیرش، تعهد میان طرف‌ها ثبت می‌شود؛ نمایش عمومی تابع روایت و مجوز بازار است.</small></div>}<button type="button" className="button button--gold hc-send-pact" disabled={mutation.isPending || !interactionId} onClick={() => mutation.mutate(undefined)}>{mutation.isPending ? 'در حال مهر و ارسال…' : 'مهر و ارسال نامه'}</button>{mutation.isError && <p className="hc-form-error">{mutation.error instanceof Error ? mutation.error.message : 'ارسال پیشنهاد انجام نشد.'}</p>}</>}</aside>
}

function presentationLocationFor(locationId: string | undefined): SceneLocation | undefined {
  if (locationId === 'haj-sadegh-office') return {
    id: locationId, name: 'دفتر حاج صادق', shortIdentity: 'دفتر بسته‌ی بازار', whyItMatters: 'غیبت حاج صادق و پاکت‌های بی‌نام از همین‌جا آغاز شده است.', currentCondition: 'در قفل است و چراغ هنوز روشن نشده.', whoIsHere: 'هیچ‌کس', recentChange: 'رد یک یادداشت مدادی و پاکتی بی‌نام روی میز مانده است.', availableActions: ['بررسی پاکت و میز'], presentationTags: ['office-closed'], state: 'action-available', businessKind: 'business', controlled: false, active: true, x: 500, y: 105,
  }
  if (locationId === 'market-entrance') return {
    id: locationId, name: 'دروازه بازار', shortIdentity: 'راه ورود بار و خبر', whyItMatters: 'نبود گاری آرد از این نقطه دیده شده است.', currentCondition: 'دروازه باز است، اما بار همیشگی صبح نرسیده.', whoIsHere: 'نگهبان بازار', recentChange: 'زنگ آغاز روز به صدا درنیامده است.', availableActions: ['پرس‌وجو از نگهبان'], presentationTags: ['entry'], state: 'quiet', businessKind: 'business', controlled: false, active: true, x: 500, y: 720,
  }
  if (locationId === 'logistics-rah-no') return {
    id: locationId, name: 'باربری راه‌نو', shortIdentity: 'مرکز ثبت و حرکت گاری‌ها', whyItMatters: 'برگه‌های اعزام این باربری آخرین رد رسمی محموله‌های آرد است.', currentCondition: 'یک گاری مهرشده هنوز در حیاط مانده و مسئول باربری مقصد آن را روشن نمی‌گوید.', whoIsHere: 'مسئول اعزام و یک گاریچی', recentChange: 'نسخه دوم یک رسید با مقصد متفاوت پیدا شده است.', availableActions: ['تطبیق رسید و مهر گاری'], presentationTags: ['logistics'], state: 'action-available', businessKind: 'logistics', controlled: false, active: true, x: 250, y: 570,
  }
  return undefined
}

function locationNarrativeFor(location: SceneLocation) {
  if (location.id === 'haj-sadegh-office') return 'رحیم می‌گوید تکه دفتر را کنار در نیمه‌باز پیدا کرده. چند شماره بار پاک شده و کنارشان نشانی یک مسیر فرعی دیده می‌شود.'
  if (location.id === 'market-entrance') return 'دفتر نگهبانی ورود بارها را ثبت کرده، اما گفته نگهبان با رد چرخ‌های تازه روی زمین جور درنمی‌آید.'
  if (location.id === 'logistics-rah-no') return 'در حیاط باربری یک گاری مهرشده مانده و دو نسخه از رسید آن، دو مقصد متفاوت نشان می‌دهند.'
  return location.currentCondition || location.shortIdentity
}

function evidenceFor(location: SceneLocation | undefined): EvidenceItem[] {
  if (!location) return []
  if (location.id === 'haj-sadegh-office') return [
    { id: 'office-erased-loads', locationId: location.id, title: 'سه ردیف پاک‌شده', sourceLabel: 'تکه دفتر آورده‌شده توسط رحیم', description: 'شماره سه محموله آرد صبح از دفتر خط خورده و کنار هر سه نشانه مسیر فرعی آمده است.', whyItMatters: 'خروج بارها ثبت شده؛ پس تأخیر ساده یا اشتباه گاریچی نیست.', certainty: 'confirmed', unlocks: ['مقایسه شماره بارها با دفتر دروازه'] },
    { id: 'office-wrong-seal', locationId: location.id, title: 'مهر با تاریخ ناخوانا', sourceLabel: 'پاکت روی میز حاج صادق', description: 'نقش مهر شبیه مهر مالی بازار است، اما تاریخ آن پاک شده و زمان صدور معلوم نیست.', whyItMatters: 'ممکن است مجوز بعداً به پرونده اضافه شده باشد؛ هنوز نمی‌شود جعل را قطعی دانست.', certainty: 'uncertain', unlocks: ['پرس‌وجو درباره اعتبار مجوز'] },
    { id: 'office-noon-note', locationId: location.id, title: 'یادداشت «تا ظهر»', sourceLabel: 'حاشیه مدادی دفتر', description: 'کنار سفارش نانوایی نوشته شده: «پیش از ظهر تسویه یا مسیر بسته شود.»', whyItMatters: 'تعویق تصمیم می‌تواند دسترسی حجره به آرد امروز را ببندد.', certainty: 'probable', unlocks: ['درخواست تضمین کوتاه‌مدت'] },
  ]
  if (location.id === 'market-entrance') return [
    { id: 'gate-route-permit', locationId: location.id, title: 'اجازه خروج از راه فرعی', sourceLabel: 'دفتر ورود دروازه', description: 'سه گاری وارد بازار شده‌اند، اما با یک اجازه موقت به راه خدماتی فرستاده شده‌اند.', whyItMatters: 'بارها به بازار رسیده‌اند و پیش از تحویل به حجره‌ها منحرف شده‌اند.', certainty: 'confirmed', unlocks: ['تطبیق مجوز با دفتر حاج صادق'] },
    { id: 'gate-wheel-tracks', locationId: location.id, title: 'حرف نگهبان و رد چرخ‌ها', sourceLabel: 'گفته نگهبان و زمین خیس دروازه', description: 'نگهبان می‌گوید گاری آردی ندیده، اما سه رد چرخ تازه به سمت راه فرعی می‌رود.', whyItMatters: 'یا نگهبان مسیر را ندیده، یا بخشی از واقعیت را پنهان می‌کند.', certainty: 'uncertain', unlocks: ['بازخواست نگهبان با شماره بار'] },
    { id: 'gate-open-window', locationId: location.id, title: 'مسیر فرعی هنوز باز است', sourceLabel: 'برنامه بستن دروازه خدماتی', description: 'راه فرعی تا نیم ساعت پیش از ظهر باز می‌ماند و بعد بدون مجوز راهبر بسته می‌شود.', whyItMatters: 'فرصت پیگیری بار محدود است؛ اقدام بی‌مدرک هم می‌تواند اختلاف عمومی بسازد.', certainty: 'probable', unlocks: ['درخواست توقف و بررسی محموله'] },
  ]
  if (location.id === 'logistics-rah-no') return [
    { id: 'logistics-double-receipt', locationId: location.id, title: 'دو رسید برای یک گاری', sourceLabel: 'دفتر اعزام باربری راه‌نو', description: 'یک شماره گاری دو رسید دارد: یکی برای نانوایی سپیده و دیگری برای انبار جنوبی.', whyItMatters: 'تغییر مقصد داخل فرایند باربری ثبت شده و قابل پیگیری است.', certainty: 'confirmed', unlocks: ['تطبیق رسمی رسید با باربری'] },
    { id: 'logistics-unbroken-seal', locationId: location.id, title: 'مهر سالم، مقصد مبهم', sourceLabel: 'گاری مانده در حیاط', description: 'مهر بار باز نشده، اما مسئول اعزام می‌گوید مقصد را شفاهی عوض کرده‌اند و نام دستوردهنده را ندارد.', whyItMatters: 'ممکن است بار هنوز دست‌نخورده باشد، ولی زنجیره دستور عمداً بی‌نام مانده است.', certainty: 'uncertain', unlocks: ['درخواست نام صادرکننده دستور'] },
    { id: 'logistics-held-cart', locationId: location.id, title: 'یک بار هنوز قابل بازگشت است', sourceLabel: 'گاریچی شیفت صبح', description: 'یکی از گاری‌ها هنوز حرکت نکرده و با تأیید دو طرف می‌تواند به مسیر اصلی برگردد.', whyItMatters: 'همکاری سریع بخشی از آرد امروز را نجات می‌دهد؛ فشار یک‌طرفه ممکن است باربری را مقابل حجره قرار دهد.', certainty: 'probable', unlocks: ['پیشنهاد همکاری برای بازگرداندن بار'] },
  ]
  return []
}

function openingStoryFor(businessName: string) {
  return [
    'صبح زنگ بازار به صدا درنیامد و گاری‌های آرد از مسیر همیشگی نرسیدند. چند حجره می‌گویند بارها وارد بازار شده‌اند، اما پیش از تحویل به راه فرعی رفته‌اند.',
    'رحیم تکه‌ای پاره از دفتر حاج صادق را آورد؛ روی آن شماره بارها خط خورده و مقصد تازه‌ای با مداد نوشته شده است. او می‌ترسد این صفحه پیش از بازشدن دفتر ناپدید شود.',
    `یکی از سفارش‌های ${businessName} میان همان ردیف‌هاست. اگر تا پیش از ظهر دلیل تغییر مسیر روشن نشود، آرد امروز به حجره نمی‌رسد و باید میان توقف کار یا خرید فوری و گران تصمیم بگیرید.`,
  ]
}

function decisionMeaningFor(investigatedIds: string[], businessName: string) {
  const hasOffice = investigatedIds.includes('haj-sadegh-office')
  const hasGate = investigatedIds.includes('market-entrance')
  const hasLogistics = investigatedIds.includes('logistics-rah-no')
  if (hasOffice && hasGate) return `مدرک ثبت و مجوز مسیر با هم جور درمی‌آیند؛ ${businessName} می‌تواند تغییر مسیر را رسمی پیگیری کند، اما هنوز راه بازگرداندن بار معلوم نیست.`
  if (hasOffice && hasLogistics) return `دفتر پاک‌شده و رسید دوگانه یک تغییر مقصد هماهنگ را نشان می‌دهند؛ ${businessName} می‌تواند برای بازگرداندن بار مذاکره کند، اما نقش دروازه مبهم می‌ماند.`
  if (hasGate && hasLogistics) return `رد چرخ‌ها و رسید دوگانه مسیر واقعی بار را روشن می‌کنند؛ ${businessName} فرصت بازگرداندن یک گاری را دارد، اما دستور اولیه هنوز بی‌نام است.`
  return `دو مدرک مستقل لازم است تا ${businessName} میان پیگیری رسمی، همکاری یا پذیرش هزینه فوری تصمیم بگیرد.`
}

function marketMood(pressure: string) {
  return ({ calm: 'اعتماد بازار برقرار است', uneasy: 'فشار بازار رو به افزایش است', strained: 'انسجام بازار شکننده است', critical: 'بازار در وضعیت بحرانی است' } as Record<string, string>)[pressure] ?? 'بازار در حال تغییر است'
}

function businessIcon(definitionId: string | undefined) {
  if (definitionId?.includes('bakery')) return 'نان'
  if (definitionId?.includes('logistics')) return 'بار'
  if (definitionId?.includes('printing')) return 'چاپ'
  if (definitionId?.includes('exchange')) return 'مهر'
  return 'چراغ'
}

function playerFacingStoryTitle(title: string | undefined, checkpoint: string | null | undefined, atmosphere: string) {
  const checkpointTitle = ({
    'morning-without-bell': 'خبر پنهانِ حجره',
    'cargo-did-not-arrive': 'بار نرسید',
    'avan-offer': 'نامه‌ای از آوان',
    'market-gathering': 'گردهمایی بازار',
    'slice-complete': 'چراغ‌های پس از تصمیم',
  } as Record<string, string>)[checkpoint ?? '']
  if (checkpointTitle) return checkpointTitle
  if (title && !/[a-z][a-z0-9]+(?:-[a-z0-9]+)+/i.test(title)) return title
  return atmosphere
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
