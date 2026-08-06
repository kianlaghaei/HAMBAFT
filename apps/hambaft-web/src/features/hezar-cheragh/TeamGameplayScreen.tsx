import { useEffect, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/client'
import { queryKeys } from '../../api/queryKeys'
import type { PublicWorld, TeamExperience } from '../../api/schemas'
import { useAuthStore } from '../../auth/authStore'
import { TeamSceneRenderer, type ActiveSceneSheet, type SceneAction, type SceneSheetContent } from '../team-scene/TeamSceneRenderer'

type UtilityPanel = 'messages' | 'pacts' | null

export function TeamGameplayScreen({ experience, world: _world, canWrite }: { experience: TeamExperience; world: PublicWorld; canWrite: boolean }) {
  const token = useAuthStore((state) => state.team?.accessToken ?? '')
  const client = useQueryClient()
  const scene = experience.teamScenePresentation
  const sceneKey = `${experience.sessionMetadata?.storyPackageId}:${experience.sessionMetadata?.storyVersion}:${scene?.sceneId ?? 'unavailable'}`
  const [activeSheet, setActiveSheet] = useState<ActiveSceneSheet>({ kind: 'opening' })
  const [backgroundUrl, setBackgroundUrl] = useState<string>()
  const [assetUnavailable, setAssetUnavailable] = useState(false)
  const [utilityPanel, setUtilityPanel] = useState<UtilityPanel>(null)
  const [notice, setNotice] = useState('')

  useEffect(() => {
    setActiveSheet({ kind: 'opening' })
    setNotice('')
    setUtilityPanel(null)
  }, [sceneKey])

  useEffect(() => {
    let objectUrl: string | undefined
    let cancelled = false
    setBackgroundUrl(undefined)
    setAssetUnavailable(false)
    if (!scene?.backgroundAssetUrl || !token) return
    void api.teamSceneAsset(token).then((asset) => {
      if (cancelled) return
      objectUrl = URL.createObjectURL(asset)
      setBackgroundUrl(objectUrl)
    }).catch(() => { if (!cancelled) setAssetUnavailable(true) })
    return () => { cancelled = true; if (objectUrl) URL.revokeObjectURL(objectUrl) }
  }, [scene?.backgroundAssetUrl, sceneKey, token])

  const investigation = useMutation({
    mutationFn: ({ sheet }: { sheet: SceneSheetContent }) => api.recordInvestigation(sheet.id, sheet.evidence.map((item) => item.id), experience.stateVersion, token),
    onSuccess: async () => {
      setActiveSheet(null)
      setNotice('بررسی ثبت شد.')
      await client.invalidateQueries({ queryKey: queryKeys.teamExperience })
    },
    onError: () => setNotice('ثبت بررسی انجام نشد. وضعیت صحنه را تازه کنید و دوباره تلاش کنید.'),
  })

  const choice = useMutation({
    mutationFn: ({ choiceId }: { choiceId: string }) => {
      const storylet = experience.privateStorylets.find((item) => !item.submitted && item.choices.some((candidate) => candidate.id === choiceId))
      if (!storylet) throw new Error('این اقدام اکنون در دسترس نیست.')
      return api.submitChoice(storylet.assignmentId, choiceId, experience.stateVersion, token)
    },
    onSuccess: async () => { setActiveSheet(null); await client.invalidateQueries({ queryKey: queryKeys.teamExperience }) },
    onError: () => setNotice('ثبت اقدام انجام نشد. وضعیت صحنه را تازه کنید و دوباره تلاش کنید.'),
  })

  if (!scene || !scene.backgroundAssetUrl || scene.hotspots.length === 0 || scene.storySheets.length === 0 || assetUnavailable) return <TeamSceneUnavailableState />

  const handleAction = (action: SceneAction, sheet?: SceneSheetContent) => {
    if (!action.available) return
    if (action.kind === 'ShowMap') { setActiveSheet(null); return }
    if (action.kind === 'OpenSheet' && action.targetId) { setActiveSheet({ kind: 'story', id: action.targetId }); return }
    if (action.kind === 'RecordInvestigation' && sheet && canWrite) { investigation.mutate({ sheet }); return }
    if (action.kind === 'OpenNextScene' && scene.nextScenePresentation) { setActiveSheet({ kind: 'next' }); return }
    if (action.kind === 'OpenPacts') { setUtilityPanel('pacts'); return }
    if (action.kind === 'SubmitChoice' && action.targetId && canWrite) { choice.mutate({ choiceId: action.targetId }); return }
    if (action.kind === 'Continue') {
      setActiveSheet(null)
      void client.invalidateQueries({ queryKey: queryKeys.teamExperience })
      void client.invalidateQueries({ queryKey: queryKeys.publicWorld(experience.sessionId) })
    }
  }
  const primaryAction = scene.authoredActions.find((action) => action.available)
  const pendingActionId = investigation.isPending
    ? scene.storySheets.flatMap((sheet) => sheet.actions).find((action) => action.kind === 'RecordInvestigation' && action.targetId === investigation.variables?.sheet.id)?.id
    : choice.isPending ? scene.authoredActions.find((action) => action.kind === 'SubmitChoice' && action.targetId === choice.variables?.choiceId)?.id : undefined
  const messages = [...(experience.inbox ?? []), ...(experience.outbox ?? [])].filter((message, index, all) => all.findIndex((candidate) => candidate.proposalId === message.proposalId) === index)
  const visibleMessages = messages.filter((message) => Boolean(message.contextualMessage))

  return <section className="hc-play-screen" dir="rtl" data-testid="team-gameplay-screen">
    <header className="hc-play-header">
      <div className="hc-play-identity"><p className="brand">HAMBAFT</p><strong>{experience.controlledEntity?.displayName ?? experience.team.displayName}</strong></div>
      <div className="hc-play-scene-title"><h1>{scene.title}</h1><span>{scene.subtitle}</span></div>
      <div className="hc-play-header-actions">
        {visibleMessages.length > 0 && <button type="button" className="hc-utility-button" onClick={() => setUtilityPanel(utilityPanel === 'messages' ? null : 'messages')}>پیام‌ها<b>{visibleMessages.length}</b></button>}
        {scene.contextualPacts.length > 0 && <button type="button" className="hc-utility-button" onClick={() => setUtilityPanel(utilityPanel === 'pacts' ? null : 'pacts')}>پیمان‌ها</button>}
      </div>
    </header>
    {notice && <div className="hc-play-notice" role="status">{notice}</div>}
    <div className="hc-play-main">
      <BusinessPanel experience={experience} action={primaryAction} pending={Boolean(pendingActionId)} onAction={handleAction} />
      <div className="hc-play-stage-column"><div className="hc-play-stage-surface">
        <TeamSceneRenderer scene={scene} backgroundUrl={backgroundUrl} activeSheet={activeSheet} pendingActionId={pendingActionId} onSelectHotspot={(id) => setActiveSheet({ kind: 'story', id })} onAction={handleAction} onCloseSheet={() => setActiveSheet(null)} />
      </div></div>
    </div>
    {utilityPanel === 'messages' && <UtilityDrawer title="پیام‌ها" onClose={() => setUtilityPanel(null)}>{visibleMessages.map((message) => <article className="hc-mini-letter" key={message.proposalId}><strong>{message.contextualMessage}</strong></article>)}</UtilityDrawer>}
    {utilityPanel === 'pacts' && <UtilityDrawer title="پیمان‌ها" onClose={() => setUtilityPanel(null)}>{scene.contextualPacts.map((pact) => <article className="hc-mini-letter" key={pact.id}><strong>{pact.title}</strong><p>{pact.message ?? pact.description}</p><small>{pact.unlock}</small></article>)}</UtilityDrawer>}
  </section>
}

function BusinessPanel({ experience, action, pending, onAction }: { experience: TeamExperience; action?: SceneAction; pending: boolean; onAction: (action: SceneAction) => void }) {
  const business = experience.businessPresentation
  return <aside className="hc-business-panel" aria-label="وضعیت کسب‌وکار شما">
    <div className="hc-business-emblem" aria-hidden="true">{business?.businessIdentity ?? ''}</div>
    <span className="eyebrow">کسب‌وکار شما</span>
    <h2>{business?.displayName ?? experience.controlledEntity?.displayName ?? experience.team.displayName}</h2>
    {business?.pulse[0] && <strong className="hc-business-condition">{business.pulse[0]}</strong>}
    <ul>{business?.pulse.slice(1, 4).map((signal) => <li key={signal}>{signal}</li>)}</ul>
    {action && <button type="button" className="button button--gold" disabled={!action.available || pending} onClick={() => onAction(action)}>{action.label}</button>}
  </aside>
}

function TeamSceneUnavailableState() {
  return <section className="hc-play-screen hc-play-screen--empty" dir="rtl" data-testid="team-scene-unavailable"><div className="hc-empty-card"><h1>صحنه در دسترس نیست</h1><p>محتوای معتبر این صحنه برای تیم شما آماده نشده است.</p></div></section>
}

function UtilityDrawer({ title, onClose, children }: { title: string; onClose: () => void; children: React.ReactNode }) {
  return <aside className="hc-utility-drawer" aria-label={title}><header><h2>{title}</h2><button type="button" className="panel-close" onClick={onClose} aria-label="بستن">×</button></header><div className="hc-drawer-list">{children}</div></aside>
}
