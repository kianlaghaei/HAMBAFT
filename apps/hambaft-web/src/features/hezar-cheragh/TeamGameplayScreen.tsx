import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { z } from 'zod'
import { api } from '../../api/client'
import { queryKeys } from '../../api/queryKeys'
import type { PublicWorld, StoryPackage, TeamExperience, TermSchema } from '../../api/schemas'
import { useAuthStore } from '../../auth/authStore'
import { usePackage } from '../../hooks/useServerQuery'
import { agreementStatusLabel, checkpointLabel, proposalStatusLabel } from '../../design-system/presentation'
import { TermsEditor, type TermDraft } from '../../components/TermsView'
import { BazaarMapViewport } from './BazaarMapViewport'
import type { SceneLocation } from '../visual-world/types'
import { buildSceneDescriptor } from '../visual-world/sceneDirector'

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
  const [pactMode, setPactMode] = useState(false)
  const [pactTargetTeamId, setPactTargetTeamId] = useState('')
  const [interactionId, setInteractionId] = useState('')
  const [terms, setTerms] = useState<TermDraft>({})
  const [notice, setNotice] = useState('')

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
  const pactTargetIds = useMemo(() => new Set(targetLocations.map((location) => location.id)), [targetLocations])
  const selectedLocation = descriptor.locations.find((location) => location.id === selectedLocationId)
  const changedLocationIds = useMemo(() => new Set(descriptor.reactions.flatMap((reaction) => reaction.locationIds)), [descriptor.reactions])
  const newInfoLocationIds = useMemo(() => new Set(descriptor.locations.filter((location) => location.availableActions.length > 0 && !investigatedIds.includes(location.id)).map((location) => location.id)), [descriptor.locations, investigatedIds])
  const mapTime = descriptor.time === 'dusk' ? 'dusk' : descriptor.time === 'night' ? 'night' : 'morning'
  const currentCheckpoint = experience.currentCheckpointId ?? world.currentCheckpointId ?? storylet?.checkpointId
  const hasInvestigation = investigatedIds.length > 0
  const choiceMutation = useMutation({
    mutationFn: ({ assignmentId, choiceId }: { assignmentId: string; choiceId: string }) => api.submitChoice(assignmentId, choiceId, experience.stateVersion, token),
    onSuccess: () => {
      setNotice('تصمیم حجره ثبت شد؛ حالا واکنش بازار را ببینید.')
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
        validityType: null,
        validForCheckpointCount: null,
        validUntilCheckpointId: null,
        expectedStateVersion: experience.stateVersion,
        commandId: crypto.randomUUID(),
      }, token)
    },
    onSuccess: () => {
      setNotice('پیام پیمان از مسیر بازار فرستاده شد؛ ردّ نامه روی نقشه می‌ماند.')
      setTerms({})
      setUtilityPanel('messages')
      void client.invalidateQueries({ queryKey: queryKeys.teamExperience })
      void client.invalidateQueries({ queryKey: queryKeys.publicWorld(experience.sessionId) })
    },
  })

  const handleLocationSelect = (location: SceneLocation) => {
    if (pactMode) {
      if (location.teamId && allowedTargetIds.has(location.teamId)) {
        setProgressSelectedLocation(location.id)
        setPactTargetTeamId(location.teamId)
        setPactMode(false)
        setUtilityPanel('pacts')
        setNotice(`حجره ${location.name} برای پیشنهاد پیمان انتخاب شد.`)
      }
      return
    }
    setProgressSelectedLocation(location.id)
  }

  const investigateSelected = () => {
    if (!selectedLocation || investigatedIds.includes(selectedLocation.id)) return
    addInvestigatedLocation(selectedLocation.id)
    setNotice(`نشانه‌ی ${selectedLocation.name} در دفتر حجره ثبت شد.`)
  }

  const openPactMode = () => {
    setUtilityPanel(null)
    setPactMode(true)
    setNotice('یک حجره‌ی روشن را روی نقشه انتخاب کنید تا نامه‌ی پیمان از همان‌جا فرستاده شود.')
  }

  const choiceLabel = storylet?.submittedChoiceId ? storylet.choices.find((choice) => choice.id === storylet.submittedChoiceId)?.presentation?.shortTitle ?? storylet.choices.find((choice) => choice.id === storylet.submittedChoiceId)?.label : undefined

  if (!storylet) {
    return <section className="hc-play-screen hc-play-screen--empty" dir="rtl"><div className="hc-empty-card"><span className="eyebrow">{checkpointLabel(currentCheckpoint)}</span><h1>{experience.entityEnding ? experience.entityEnding.title : 'بازار در سکوتِ بین دو پرده'}</h1><p>{experience.entityEnding?.paragraphs[0] ?? 'پرده‌ی بعدی هنوز برای این حجره باز نشده است.'}</p><Link className="button button--gold" to="/team/status">دیدن وضعیت حجره</Link></div></section>
  }

  return <section className={`hc-play-screen${pactMode ? ' is-pact-mode' : ''}`} dir="rtl" data-testid="team-gameplay-screen">
    <header className="hc-play-header">
      <div className="hc-play-identity">
        <p className="brand">HAMBAFT <span>/ هزارچراغ</span></p>
        <div><strong>{experience.controlledEntity?.displayName ?? experience.team.displayName}</strong><span className="hc-play-divider">|</span><span>{descriptor.atmosphereLabel}</span></div>
      </div>
      <div className="hc-play-scene-title"><span className="hc-play-kicker">{checkpointLabel(currentCheckpoint)}</span><h1>{storylet.title}</h1></div>
      <div className="hc-play-header-actions">
        <span className={`hc-semantic-chip pressure-${descriptor.pressure}`}>{marketMood(descriptor.pressure)}</span>
        <button type="button" className={`hc-utility-button${utilityPanel === 'messages' ? ' is-active' : ''}`} aria-pressed={utilityPanel === 'messages'} onClick={() => setUtilityPanel((current) => current === 'messages' ? null : 'messages')}>پیام‌ها{experience.inbox?.length ? <b>{experience.inbox.length}</b> : null}</button>
        <button type="button" className={`hc-utility-button${utilityPanel === 'pacts' ? ' is-active' : ''}`} aria-pressed={utilityPanel === 'pacts'} onClick={() => setUtilityPanel((current) => current === 'pacts' ? null : 'pacts')}>پیمان‌ها{experience.agreements?.length ? <b>{experience.agreements.length}</b> : null}</button>
      </div>
    </header>

    {notice && <div className="hc-play-notice" role="status" aria-live="polite">{notice}</div>}
    <div className="hc-play-main">
      <section className="hc-play-map-column" aria-label="نقشه‌ی تعاملی بازار">
        <div className="hc-map-heading"><div><span className="eyebrow">جهانِ تصمیم</span><h2>بازار را از روی نشانه‌ها بخوانید</h2></div><span className={`hc-map-mode${pactMode ? ' is-pact' : ''}`}>{pactMode ? 'یک مقصد برای پیمان انتخاب کنید' : `${investigatedIds.length ? 'نشانه‌ها روی نقشه مانده‌اند' : 'نقطه‌ای را برای بررسی انتخاب کنید'}`}</span></div>
        <div className="hc-play-map-canvas"><BazaarMapViewport descriptor={descriptor} timeMode={mapTime} selectedLocationId={selectedLocationId} pactTargetIds={pactTargetIds} changedLocationIds={changedLocationIds} newInfoLocationIds={newInfoLocationIds} onSelectLocation={handleLocationSelect} /></div>
        <div className="hc-map-legend" aria-label="راهنمای نقشه"><span><i className="legend-dot legend-dot--new" />نشانه‌ی تازه</span><span><i className="legend-dot legend-dot--pact" />مقصد پیمان</span><span><i className="legend-line" />رابطه و اثر پیمان</span></div>
      </section>

      <aside className="hc-play-panel" aria-label="روایت و اقدام پرده">
        <div className="hc-panel-scroll-safe">
          <div className="hc-narrative-block"><div className="hc-narrative-meta"><span className="eyebrow">روایت جاری</span>{storylet.presentationTags.speaker && <span>{storylet.presentationTags.speaker}</span>}</div><h2>{storylet.title}</h2><div className="hc-narrative-copy">{storylet.paragraphs.map((paragraph) => <p key={paragraph}>{paragraph}</p>)}</div></div>
          <div className="hc-objective-card"><span className="eyebrow">هدف این پرده</span><strong>{objectiveFor(currentCheckpoint, hasInvestigation, storylet.submitted)}</strong><div className="hc-step-row"><span className="is-done">خبر</span><span className={hasInvestigation ? 'is-done' : 'is-current'}>بررسی بازار</span><span className={storylet.submitted ? 'is-done' : hasInvestigation ? 'is-current' : ''}>انتخاب حجره</span><span>واکنش بازار</span></div></div>
          <div className="hc-active-action"><div className="hc-action-heading"><div><span className="eyebrow">اقدام فعال</span><h2>{storylet.submitted ? 'تصمیم در بازار افتاد' : hasInvestigation ? 'حالا مسیر حجره را انتخاب کنید' : 'اول یک نشانه پیدا کنید'}</h2></div><span className="hc-action-count">{hasInvestigation ? 'دفتر روشن' : 'نقشه آماده است'}</span></div>
            {selectedLocation && <LocationContext location={selectedLocation} investigated={investigatedIds.includes(selectedLocation.id)} onInvestigate={investigateSelected} canWrite={canWrite && !storylet.submitted} />}
            {!selectedLocation && !storylet.submitted && <p className="hc-action-hint">از روی نقشه یکی از نقاط روشن را انتخاب کنید. هر بررسی، یک تکه از داستان «بار نرسید» را برای حجره‌تان روشن می‌کند.</p>}
            {storylet.submitted ? <div className="hc-decision-result"><span className="hc-result-mark">✓</span><div><strong>{choiceLabel ?? 'تصمیم حجره ثبت شد'}</strong><p>بازار حالا پیامد این تصمیم را در خود نشان می‌دهد. برای ادامه‌ی پرده، به نشانه‌های پایین نقشه نگاه کنید.</p></div></div> : hasInvestigation && <ChoiceCards storylet={storylet} disabled={!canWrite || choiceMutation.isPending} onChoose={(choiceId) => choiceMutation.mutate({ assignmentId: storylet.assignmentId, choiceId })} />}
            {choiceMutation.error && <p className="hc-form-error">{choiceMutation.error.message}</p>}
          </div>
        </div>
      </aside>
    </div>

    <SceneBottomStrip descriptor={descriptor} experience={experience} onPacts={openPactMode} />
    {utilityPanel === 'messages' && <MessagesDrawer experience={experience} world={world} onClose={() => setUtilityPanel(null)} />}
    {utilityPanel === 'pacts' && <PactsDrawer experience={experience} packageData={packageQuery.data} targetLocations={targetLocations} selectedTeamId={pactTargetTeamId} interactionId={activeInteractionId} terms={terms} mutation={pactMutation} onClose={() => setUtilityPanel(null)} onPickOnMap={openPactMode} onTargetSelect={(location) => { setProgressSelectedLocation(location.id); setPactTargetTeamId(location.teamId ?? '') }} onInteractionChange={(id) => { setInteractionId(id); setTerms({}) }} onTermsChange={setTerms} />}
  </section>
}

function LocationContext({ location, investigated, onInvestigate, canWrite }: { location: SceneLocation; investigated: boolean; onInvestigate: () => void; canWrite: boolean }) {
  return <div className="hc-location-context"><div><span className="eyebrow">نقطه‌ی انتخاب‌شده</span><h3>{location.name}</h3></div><p>{location.currentCondition}</p>{location.recentChange && <small>{location.recentChange}</small>}{location.availableActions.length > 0 && <span className="hc-location-action">{location.availableActions[0]}</span>}<button type="button" className={`button ${investigated ? 'button--ghost' : 'button--gold'}`} disabled={investigated || !canWrite} onClick={onInvestigate}>{investigated ? 'نشانه ثبت شد' : 'بررسی این نقطه'}</button></div>
}

function ChoiceCards({ storylet, disabled, onChoose }: { storylet: Storylet; disabled: boolean; onChoose: (choiceId: string) => void }) {
  return <div className="hc-choice-grid" aria-label="انتخاب‌های این پرده">{storylet.choices.map((choice) => { const presentation = choice.presentation; return <button type="button" key={choice.id} className="hc-choice-card" disabled={disabled} onClick={() => onChoose(choice.id)}><span className="hc-choice-title">{presentation?.shortTitle ?? choice.label}</span><span>{presentation?.action ?? choice.shortOutcome ?? 'این انتخاب مسیر حجره را تغییر می‌دهد.'}</span>{presentation?.knownRisk && <small>ریسک: {presentation.knownRisk}</small>}</button> })}</div>
}

function SceneBottomStrip({ descriptor, experience, onPacts }: { descriptor: ReturnType<typeof buildSceneDescriptor>; experience: TeamExperience; onPacts: () => void }) {
  const proposals = [...(experience.inbox ?? []), ...(experience.outbox ?? [])].filter((proposal, index, all) => all.findIndex((item) => item.proposalId === proposal.proposalId) === index).slice(0, 2)
  const agreements = experience.agreements ?? []
  const semanticState = experience.worldPresentation?.semanticMetrics ?? []
  return <footer className="hc-scene-footer"><div className="hc-footer-block"><span className="eyebrow">سیگنال‌های اخیر</span><p>{descriptor.pulse[0] ?? descriptor.publicEvent ?? 'بازار هنوز آرام است.'}</p><small>{descriptor.businessPulse[0] ?? 'حرکت حجره‌ها روی نقشه خوانده می‌شود.'}</small></div><div className="hc-footer-block hc-footer-pacts"><span className="eyebrow">پیمان‌های فعال</span>{agreements.length ? <div className="hc-footer-items">{agreements.slice(0, 2).map((agreement) => <span key={agreement.agreementId}>{agreementStatusLabel(agreement.status)} · پیمان بازار</span>)}</div> : <button type="button" className="hc-inline-action" onClick={onPacts}>ارسال نامه از روی نقشه</button>}</div><div className="hc-footer-block"><span className="eyebrow">آخرین پیام‌ها</span>{proposals.length ? <div className="hc-footer-items">{proposals.map((proposal) => <span key={proposal.proposalId}>{proposalStatusLabel(proposal.status)} · نامه‌ی بازار</span>)}</div> : <small>خبر تازه‌ای در صندوق نیست.</small>}</div><div className="hc-footer-block hc-footer-state"><span className="eyebrow">حالت حجره و جهان</span><strong>{marketMood(descriptor.pressure)}</strong><small>{semanticState[0]?.description ?? 'اعتماد و فشار از رفتار بازار خوانده می‌شود، نه از عدد.'}</small></div></footer>
}

function MessagesDrawer({ experience, world, onClose }: { experience: TeamExperience; world: PublicWorld; onClose: () => void }) {
  const proposals = [...(experience.inbox ?? []), ...(experience.outbox ?? [])].filter((proposal, index, all) => all.findIndex((item) => item.proposalId === proposal.proposalId) === index).slice(0, 4)
  const teamName = (teamId: string) => world.entities.find((entity) => entity.controlledByTeamId === teamId)?.displayName ?? 'حجره‌ی دیگر'
  return <aside className="hc-utility-drawer" aria-label="پیام‌های بازار"><header><div><span className="eyebrow">ارتباط در دل بازار</span><h2>پیام‌های رسیده و فرستاده‌شده</h2></div><button type="button" className="panel-close" onClick={onClose} aria-label="بستن پیام‌ها">×</button></header>{proposals.length ? <div className="hc-drawer-list">{proposals.map((proposal) => <article className="hc-mini-letter" key={proposal.proposalId}><div><strong>{teamName(proposal.senderTeamId)} ← {teamName(proposal.receiverTeamId)}</strong><span>{proposalStatusLabel(proposal.status)}</span></div><p>نامه‌ی همکاری در مسیر حجره‌هاست.</p></article>)}</div> : <p className="hc-drawer-empty">هنوز نامه‌ای میان حجره‌ها رد و بدل نشده است.</p>}<Link className="button button--ghost" to="/team/messages">باز کردن دفتر کامل پیام‌ها</Link></aside>
}

function PactsDrawer({ experience, packageData, targetLocations, selectedTeamId, interactionId, terms, mutation, onClose, onPickOnMap, onTargetSelect, onInteractionChange, onTermsChange }: { experience: TeamExperience; packageData?: StoryPackage; targetLocations: SceneLocation[]; selectedTeamId: string; interactionId: string; terms: TermDraft; mutation: UseMutationResult<unknown, Error, void, unknown>; onClose: () => void; onPickOnMap: () => void; onTargetSelect: (location: SceneLocation) => void; onInteractionChange: (id: string) => void; onTermsChange: (terms: TermDraft) => void }) {
  const interaction = packageData?.interactions.find((item) => item.id === interactionId)
  const target = targetLocations.find((location) => location.teamId === selectedTeamId)
  return <aside className="hc-utility-drawer hc-pacts-drawer" aria-label="پیمان‌های بازار"><header><div><span className="eyebrow">همکاری از روی نقشه</span><h2>{target ? `نامه برای ${target.name}` : 'پیمان‌های بازار'}</h2></div><button type="button" className="panel-close" onClick={onClose} aria-label="بستن پیمان‌ها">×</button></header>{!target ? <><p className="hc-drawer-intro">مقصد را از خود بازار انتخاب کنید؛ خط رابطه بعد از ارسال روی نقشه دیده می‌شود.</p><button type="button" className="button button--gold" onClick={onPickOnMap}>انتخاب مقصد روی نقشه</button><div className="hc-target-list">{targetLocations.map((location) => <button type="button" key={location.id} className="hc-target-row" onClick={() => onTargetSelect(location)}><strong>{location.name}</strong><span>{location.currentCondition}</span></button>)}</div></> : <><button type="button" className="hc-back-link" onClick={onPickOnMap}>← تغییر مقصد روی نقشه</button><div className="hc-pact-target"><span className="eyebrow">مقصد انتخاب‌شده</span><strong>{target.name}</strong><small>{target.currentCondition}</small></div>{experience.availableInteractionTypes && experience.availableInteractionTypes.length > 1 && <label className="hc-drawer-field"><span>نوع نامه</span><select value={interactionId} onChange={(event) => onInteractionChange(event.target.value)}>{experience.availableInteractionTypes.map((available) => <option key={available.interactionTypeId} value={available.interactionTypeId}>{packageData?.interactions.find((item) => item.id === available.interactionTypeId)?.displayName ?? available.interactionTypeId}</option>)}</select></label>}{interaction && <><p className="hc-drawer-intro">{interaction.description}</p><TermsEditor schema={interaction.termsSchema} value={terms} onChange={onTermsChange} /></>}<button type="button" className="button button--gold hc-send-pact" disabled={mutation.isPending || !interactionId} onClick={() => mutation.mutate(undefined)}>{mutation.isPending ? 'در حال فرستادن…' : 'فرستادن نامه‌ی پیمان'}</button>{mutation.isError && <p className="hc-form-error">{mutation.error instanceof Error ? mutation.error.message : 'ارسال پیشنهاد انجام نشد.'}</p>}</>}<Link className="button button--ghost hc-drawer-link" to="/team/agreements">دیدن دفتر پیمان‌ها</Link></aside>
}

function objectiveFor(checkpoint: string | null | undefined, investigated: boolean, submitted: boolean) {
  if (submitted) return 'واکنش بازار را از روی تغییر چراغ‌ها و پیام‌های تازه دنبال کنید.'
  if (checkpoint === 'cargo-did-not-arrive') return investigated ? 'از نشانه‌های بار نرسیده استفاده کنید و مسیر حجره را ثبت کنید.' : 'پیش از انتخاب، یک تا سه نشانه از مسیر بار و حجره‌های درگیر پیدا کنید.'
  if (checkpoint === 'avan-offer') return investigated ? 'پیشنهاد آوان را با وضعیت حجره‌ی خود بسنجید.' : 'نقاط درگیر پیشنهاد آوان را روی نقشه بخوانید.'
  return investigated ? 'با دانستن حال بازار، تصمیم حجره را ثبت کنید.' : 'یک نشانه از بازار پیدا کنید و بعد تصمیم بگیرید.'
}

function marketMood(pressure: string) {
  return ({ calm: 'بازار آرام است', uneasy: 'راه هنوز نامطمئن است', strained: 'جنب‌وجوش میدان کم شده', critical: 'خبر در بازار پیچیده است' } as Record<string, string>)[pressure] ?? 'بازار در حال تغییر است'
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
