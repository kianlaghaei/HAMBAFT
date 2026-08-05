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
import { BusinessActionCard, DecisionSummary, EvidenceReveal, LocationInvestigationCard, MarketReactionCard, StoryOpeningCard, StoryProgress, type TeamEvidenceCard, type TeamStoryPhase } from './TeamStoryPresentation'
import type { SceneLocation } from '../visual-world/types'
import { buildSceneDescriptor } from '../visual-world/sceneDirector'
import { useUiStore } from '../../state/uiStore'

type Storylet = TeamExperience['privateStorylets'][number]
type UtilityPanel = 'messages' | 'pacts' | null

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
  const addInvestigatedLocation = (id: string) => setSceneProgress((current) => ({ key: sceneKey, selectedLocationId: current.key === sceneKey ? current.selectedLocationId : id, investigatedIds: [...(current.key === sceneKey ? current.investigatedIds : []), id].slice(-3) }))
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
  const relevantMarkerIds = new Set(['haj-sadegh-office', 'market-entrance', controlledLocation?.id].filter((id): id is string => Boolean(id)))
  const inactiveLocationIds = new Set(descriptor.locations.filter((location) => !location.active).map((location) => location.id))
  const disabledMarkerLocationIds = TEAM_MARKET_MARKERS.filter((marker) => !relevantMarkerIds.has(marker.id) || inactiveLocationIds.has(marker.id)).map((marker) => marker.id)
  const handleMarkerSelect = (locationId?: string) => {
    if (locationId && relevantMarkerIds.has(locationId)) {
      setProgressSelectedLocation(locationId)
      setStoryPhase(investigatedIds.includes(locationId) ? 'evidence' : 'investigation')
      return
    }
    if (!locationId) {
      setSceneProgress((current) => ({ ...current, key: sceneKey, selectedLocationId: undefined }))
      setStoryPhase('opening')
    }
  }
  const openingParagraphs = [...new Set([...(world.narrative?.paragraphs ?? []), ...(storylet?.paragraphs ?? [])])].slice(0, 3)
  const evidence = evidenceFor(selectedLocation, storylet)
  const businessName = experience.businessPresentation?.displayName ?? experience.controlledEntity?.displayName ?? experience.team.displayName
  const businessSummary = experience.businessPresentation?.pulse[0] ?? storylet?.paragraphs.at(-1) ?? 'این خبر روی تصمیم امروز حجره شما اثر مستقیم دارد.'
  const businessMeaning = storylet?.paragraphs.at(-1) ?? experience.businessPresentation?.pulse[0] ?? 'باید میان حفظ اختیار حجره و شریک‌کردن بازار در این خبر تصمیم بگیرید.'
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
    if (!selectedLocation || investigatedIds.includes(selectedLocation.id)) return
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
        <button type="button" className={`hc-utility-button${utilityPanel === 'messages' ? ' is-active' : ''}`} aria-pressed={utilityPanel === 'messages'} onClick={() => setUtilityPanel((current) => current === 'messages' ? null : 'messages')}>پیام‌ها{experience.inbox?.length ? <b>{experience.inbox.length}</b> : null}</button>
        <button type="button" className={`hc-utility-button${utilityPanel === 'pacts' ? ' is-active' : ''}`} aria-pressed={utilityPanel === 'pacts'} onClick={() => setUtilityPanel((current) => current === 'pacts' ? null : 'pacts')}>پیمان‌ها{experience.agreements?.length ? <b>{experience.agreements.length}</b> : null}</button>
      </div>
    </header>

    {notice && <div className="hc-play-notice" role="status" aria-live="polite">{notice}</div>}
    <div className="hc-play-main">
      <BusinessPanel experience={experience} descriptor={descriptor} onOpen={() => { setStoryPhase('business'); if (controlledLocation) setProgressSelectedLocation(controlledLocation.id) }} />
      <div className="hc-play-stage-column">
        <div className="hc-play-stage-surface"><EmptyMarketStage ownedLocationId={controlledLocation?.id} activeLocationId={activeMarkerLocationId} selectedLocationId={selectedLocationId} urgentLocationIds={urgentMarkerLocationIds} disabledLocationIds={disabledMarkerLocationIds} onSelectLocation={handleMarkerSelect} /></div>
      </div>

      <aside className={`hc-play-panel sheet-${sheetState}`} aria-label="روایت و اقدام پرده" data-mode={currentReaction ? 'WorldReaction' : storyPhase}>
        <div className="hc-sheet-controls" role="group" aria-label="اندازه پنل بازی">{(['collapsed', 'half', 'expanded'] as const).map((state) => <button key={state} type="button" className={sheetState === state ? 'is-active' : ''} aria-pressed={sheetState === state} onClick={() => setSheetState(state)}>{state === 'collapsed' ? 'جمع' : state === 'half' ? 'نیمه' : 'باز'}</button>)}</div>
        <div className="hc-panel-scroll-safe">
          <StoryProgress phase={currentReaction ? 'reaction' : storyPhase} />
          <div className="hc-active-action">
            {currentReaction && <WorldReactionStep reaction={currentReaction} index={reactionIndex} total={descriptor.reactions.length} onNext={() => { if (reactionIndex >= descriptor.reactions.length - 1) { markReactionSeen(world.id, descriptor.visualVersion); setReactionIndex(-1) } else setReactionIndex((current) => current + 1) }} onSkip={() => { markReactionSeen(world.id, descriptor.visualVersion); setReactionIndex(-1) }} />}
            {!currentReaction && storyPhase === 'opening' && <StoryOpeningCard title={eventTitle} speaker={storylet.presentationTags.speaker} paragraphs={openingParagraphs} instruction="روی نشان «دفتر حاج صادق» بزنید و پاکت بی‌نام را بررسی کنید." onContinue={() => { setProgressSelectedLocation('haj-sadegh-office'); setStoryPhase(investigatedIds.includes('haj-sadegh-office') ? 'evidence' : 'investigation') }} />}
            {!currentReaction && storyPhase === 'investigation' && selectedLocation && <LocationInvestigationCard title={selectedLocation.name} identity={selectedLocation.shortIdentity} narrative={locationNarrativeFor(selectedLocation, storylet)} actionLabel={investigatedIds.includes(selectedLocation.id) ? 'نشانه ثبت شده است' : 'بررسی نشانه‌ها'} onInvestigate={investigateSelected} />}
            {!currentReaction && storyPhase === 'evidence' && <EvidenceReveal evidence={evidence} message={evidenceMessageFor(selectedLocation)} onContinue={() => { setStoryPhase('business'); if (controlledLocation) setProgressSelectedLocation(controlledLocation.id) }} />}
            {!currentReaction && storyPhase === 'business' && <BusinessActionCard businessName={businessName} summary={businessSummary} implication={businessMeaning} canOfferPact={availableInteractions.length > 0 && targetLocations.length > 0} onOpenPact={() => setUtilityPanel('pacts')} onContinue={() => setStoryPhase('decision')} />}
            {!currentReaction && storyPhase === 'decision' && <><DecisionSummary finding={storylet.paragraphs[1] ?? evidence[0]?.body ?? 'بخشی از دفتر حاج صادق به حجره شما رسیده است.'} businessMeaning={businessMeaning} /><ChoiceCards storylet={storylet} disabled={!canWrite || choiceMutation.isPending} onChoose={(choiceId) => { setChosenChoiceId(choiceId); choiceMutation.mutate({ assignmentId: storylet.assignmentId, choiceId }) }} /></>}
            {!currentReaction && storyPhase === 'reaction' && <MarketReactionCard choiceLabel={choiceLabel} reaction={reactionText} waiting={!descriptor.reactions.length} onContinue={continueToNextScene} />}
            {choiceMutation.error && <p className="hc-form-error">{choiceMutation.error.message}</p>}
          </div>
        </div>
      </aside>
    </div>

    <SceneBottomStrip descriptor={descriptor} experience={experience} onPacts={() => setUtilityPanel('pacts')} />
    {utilityPanel === 'messages' && <MessagesDrawer experience={experience} world={world} onClose={() => setUtilityPanel(null)} />}
    {utilityPanel === 'pacts' && <PactsDrawer experience={experience} packageData={packageQuery.data} targetLocations={targetLocations} selectedTeamId={pactTargetTeamId} interactionId={activeInteractionId} terms={terms} validForCheckpoints={validForCheckpoints} mutation={pactMutation} onClose={() => setUtilityPanel(null)} onTargetSelect={(location) => { setProgressSelectedLocation(location.id); setPactTargetTeamId(location.teamId ?? '') }} onInteractionChange={(id) => { setInteractionId(id); setTerms({}) }} onTermsChange={setTerms} onValidityChange={setValidForCheckpoints} />}
  </section>
}

function BusinessPanel({ experience, descriptor, onOpen }: { experience: TeamExperience; descriptor: ReturnType<typeof buildSceneDescriptor>; onOpen: () => void }) {
  const business = experience.businessPresentation
  const signals = business?.pulse.slice(0, 3) ?? descriptor.businessPulse.slice(0, 3)
  return <aside className="hc-business-panel" aria-label="وضعیت کسب‌وکار شما">
    <div className="hc-business-emblem" aria-hidden="true">{businessIcon(business?.entityDefinitionId ?? experience.controlledEntity?.definitionId)}</div>
    <span className="eyebrow">کسب‌وکار شما</span>
    <h2>{business?.displayName ?? experience.controlledEntity?.displayName ?? experience.team.displayName}</h2>
    <strong className="hc-business-condition">{signals[0] ?? 'چراغ حجره روشن و آماده‌ی تصمیم است.'}</strong>
    <ul>{signals.slice(1).map((signal) => <li key={signal}>{signal}</li>)}{signals.length < 2 && <li>خبرهای مهم حجره در همین‌جا می‌مانند.</li>}</ul>
    <button type="button" className="button button--gold" onClick={onOpen}>ورود به فعالیت کسب‌وکار</button>
    <details><summary>جزئیات بیشتر</summary><p>وضعیت کامل حجره بدون نمایش عددهای فنی در همین صفحه در دسترس است.</p></details>
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

function SceneBottomStrip({ descriptor, experience, onPacts }: { descriptor: ReturnType<typeof buildSceneDescriptor>; experience: TeamExperience; onPacts: () => void }) {
  const proposals = [...(experience.inbox ?? []), ...(experience.outbox ?? [])].filter((proposal, index, all) => all.findIndex((item) => item.proposalId === proposal.proposalId) === index).slice(0, 2)
  const agreements = experience.agreements ?? []
  const semanticState = experience.worldPresentation?.semanticMetrics ?? []
  return <footer className="hc-scene-footer"><div className="hc-footer-block"><span className="eyebrow">سیگنال‌های اخیر</span><p>{descriptor.pulse[0] ?? descriptor.publicEvent ?? 'بازار هنوز آرام است.'}</p><small>{descriptor.businessPulse[0] ?? 'حرکت حجره‌ها از نشانه‌های بازار خوانده می‌شود.'}</small></div><div className="hc-footer-block hc-footer-pacts"><span className="eyebrow">پیمان‌های فعال</span>{agreements.length ? <div className="hc-footer-items">{agreements.slice(0, 2).map((agreement) => <span key={agreement.agreementId}>{agreementStatusLabel(agreement.status)} · پیمان بازار</span>)}</div> : <button type="button" className="hc-inline-action" onClick={onPacts}>ارسال نامه برای یک حجره</button>}</div><div className="hc-footer-block"><span className="eyebrow">آخرین پیام‌ها</span>{proposals.length ? <div className="hc-footer-items">{proposals.map((proposal) => <span key={proposal.proposalId}>{proposalStatusLabel(proposal.status)} · نامه‌ی بازار</span>)}</div> : <small>خبر تازه‌ای در صندوق نیست.</small>}</div><div className="hc-footer-block hc-footer-state"><span className="eyebrow">حالت حجره و جهان</span><strong>{marketMood(descriptor.pressure)}</strong><small>{semanticState[0]?.description ?? 'اعتماد و فشار از رفتار بازار خوانده می‌شود، نه از عدد.'}</small></div></footer>
}

function MessagesDrawer({ experience, world, onClose }: { experience: TeamExperience; world: PublicWorld; onClose: () => void }) {
  const proposals = [...(experience.inbox ?? []), ...(experience.outbox ?? [])].filter((proposal, index, all) => all.findIndex((item) => item.proposalId === proposal.proposalId) === index).slice(0, 4)
  const teamName = (teamId: string) => world.entities.find((entity) => entity.controlledByTeamId === teamId)?.displayName ?? 'حجره‌ی دیگر'
  return <aside className="hc-utility-drawer" aria-label="پیام‌های بازار"><header><div><span className="eyebrow">ارتباط در دل بازار</span><h2>پیام‌های رسیده و فرستاده‌شده</h2></div><button type="button" className="panel-close" onClick={onClose} aria-label="بستن پیام‌ها">×</button></header>{proposals.length ? <div className="hc-drawer-list">{proposals.map((proposal) => <article className="hc-mini-letter" key={proposal.proposalId}><div><strong>{teamName(proposal.senderTeamId)} ← {teamName(proposal.receiverTeamId)}</strong><span>{proposalStatusLabel(proposal.status)}</span></div><p>نامه‌ی همکاری در مسیر حجره‌هاست.</p></article>)}</div> : <p className="hc-drawer-empty">هنوز نامه‌ای میان حجره‌ها رد و بدل نشده است.</p>}<p className="hc-drawer-empty">پیام‌ها در همین صفحه پیگیری می‌شوند.</p></aside>
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
  return undefined
}

function locationNarrativeFor(location: SceneLocation, storylet: Storylet) {
  if (location.id === 'haj-sadegh-office') return 'درِ دفتر باز نمی‌شود. از پشت شیشه، پاکتی بی‌نام و جای استکان دیشب دیده می‌شود؛ انگار صاحب دفتر با عجله رفته است.'
  if (location.id === 'market-entrance') return 'نگهبان می‌گوید صبح شروع شده، اما نه زنگ بازار را شنیده و نه گاری آردی را دیده است. این سکوت برای حجره شما عادی نیست.'
  if (location.controlled) return storylet.paragraphs[0] ?? `${location.name} منتظر خبری است که تصمیم امروز را روشن کند.`
  return location.currentCondition || location.shortIdentity
}

function evidenceFor(location: SceneLocation | undefined, storylet: Storylet | undefined): TeamEvidenceCard[] {
  const fragment = storylet?.paragraphs[1] ?? 'بخشی از دفتر حاج صادق به حجره شما رسیده و بعضی حساب‌های قدیمی را روشن می‌کند.'
  const businessImpact = storylet?.paragraphs[2] ?? 'این خبر می‌تواند تصمیم امروز حجره را عوض کند.'
  if (location?.id === 'market-entrance') return [
    { kind: 'seal', title: 'زنگ خاموش', body: 'نگهبان تأیید می‌کند زنگ آغاز بازار امروز به صدا درنیامده است.' },
    { kind: 'document', title: 'ورود ثبت‌نشده', body: 'نام گاری همیشگی آرد در دفتر ورود صبح دیده نمی‌شود.' },
  ]
  return [
    { kind: 'letter', title: 'پاکت بی‌نام', body: fragment },
    { kind: 'document', title: 'تکه دفتر', body: businessImpact },
    { kind: 'seal', title: 'رد حاج صادق', body: 'کنار نوشته‌ها یک جمله با مداد مانده است: «هر بدهی، عدد نیست.»' },
  ]
}

function evidenceMessageFor(location: SceneLocation | undefined) {
  if (location?.id === 'market-entrance') return 'بار نرسیده، اما کسی هنوز بسته‌شدن راه را رسمی اعلام نکرده است.'
  return 'هر بدهی، عدد نیست. بعضی حساب‌ها با اعتماد، نان یا یک قول قدیمی بسته شده‌اند.'
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
