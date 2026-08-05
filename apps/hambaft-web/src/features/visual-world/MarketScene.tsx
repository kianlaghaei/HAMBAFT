import { lazy, Suspense, useCallback, useEffect, useMemo, useRef, useState, type CSSProperties } from 'react'
import type { PublicWorld, TeamExperience } from '../../api/schemas'
import { useUiStore } from '../../state/uiStore'
import { marketAudio } from './audio'
import { FallbackMarket } from './FallbackMarket'
import { buildSceneDescriptor } from './sceneDirector'
import type { SceneLocation } from './types'
import { marketDiagnostics, useMarketDiagnostics } from './diagnostics'

const PixiMarketCanvas = lazy(() => import('./PixiMarketCanvas').then((module) => ({ default: module.PixiMarketCanvas })))
const introRoute = ['market-entrance', 'central-crossroads', 'clock-courtyard', 'haj-sadegh-office']

export function MarketScene({ world, team, display = false, onSelectTeam }: { world: PublicWorld; team?: TeamExperience; display?: boolean; onSelectTeam?: (teamId: string) => void }) {
  const preferredMode = useUiStore((state) => state.visualMode)
  const setVisualMode = useUiStore((state) => state.setVisualMode)
  const audioEnabled = useUiStore((state) => state.audioEnabled)
  const setAudioEnabled = useUiStore((state) => state.setAudioEnabled)
  const systemReduced = useUiStore((state) => state.reducedMotion)
  const inspected = useUiStore((state) => state.inspectedLocations)
  const inspectLocation = useUiStore((state) => state.inspectLocation)
  const introductionDone = useUiStore((state) => state.completedIntroductions[world.id] ?? false)
  const completeIntroduction = useUiStore((state) => state.completeIntroduction)
  const seenReactionVersion = useUiStore((state) => state.seenReactionVersions[world.id])
  const markReactionSeen = useUiStore((state) => state.markReactionSeen)
  const [pixiFailed, setPixiFailed] = useState(false)
  const [selectedId, setSelectedId] = useState<string>()
  const [introIndex, setIntroIndex] = useState(!display && !introductionDone && team?.worldPresentation ? 0 : -1)
  const [reactionIndex, setReactionIndex] = useState(-1)
  const diagnostics = useMarketDiagnostics()
  const panelRef = useRef<HTMLElement>(null)
  const descriptor = useMemo(() => buildSceneDescriptor(world, team), [team, world])
  const mode = systemReduced && preferredMode === 'high' ? 'reduced' : pixiFailed ? 'fallback' : preferredMode
  const onFailure = useCallback(() => setPixiFailed(true), [])
  const ownLocation = descriptor.locations.find((location) => location.entityId === team?.controlledEntity?.id)?.id
  const introIds = [...introRoute, ...(ownLocation ? [ownLocation] : [])]
  const introLocation = introIndex >= 0 ? descriptor.locations.find((location) => location.id === introIds[introIndex]) : undefined
  const selected = descriptor.locations.find((location) => location.id === selectedId)
  const visibleDescriptor = useMemo(() => ({ ...descriptor, connections: descriptor.connections.filter((connection) => connection.kind === 'agreement' || !selected?.entityId || connection.fromEntityId === selected.entityId || connection.toEntityId === selected.entityId) }), [descriptor, selected])
  const style = introLocation ? ({ '--camera-x': `${50 - introLocation.x / 10}%`, '--camera-y': `${50 - introLocation.y / 7}%` } as CSSProperties) : undefined

  useEffect(() => () => marketAudio.stop(), [])
  useEffect(() => { marketDiagnostics.update({ activeSceneId: descriptor.id, ambientEntityCount: Math.min(24, descriptor.ambientEvents.length), fallbackState: mode === 'fallback' }) }, [descriptor.ambientEvents.length, descriptor.id, mode])
  useEffect(() => {
    if (introIndex < 0 || systemReduced) return
    const timer = window.setTimeout(() => {
      if (introIndex >= introIds.length - 1) { completeIntroduction(world.id); setIntroIndex(-1) }
      else setIntroIndex((value) => value + 1)
    }, 4_000)
    return () => window.clearTimeout(timer)
  }, [completeIntroduction, introIds.length, introIndex, systemReduced, world.id])
  useEffect(() => {
    if (seenReactionVersion === undefined) { markReactionSeen(world.id, descriptor.visualVersion); return }
    if (descriptor.visualVersion <= seenReactionVersion || !descriptor.reactions.length) return
    const timer = window.setTimeout(() => setReactionIndex(0), 0)
    return () => window.clearTimeout(timer)
  }, [descriptor.reactions.length, descriptor.visualVersion, markReactionSeen, seenReactionVersion, world.id])
  useEffect(() => {
    if (reactionIndex < 0 || systemReduced) return
    const timer = window.setTimeout(() => {
      if (reactionIndex >= descriptor.reactions.length - 1) { markReactionSeen(world.id, descriptor.visualVersion); setReactionIndex(-1) }
      else setReactionIndex((value) => value + 1)
    }, 4_000)
    return () => window.clearTimeout(timer)
  }, [descriptor.reactions.length, descriptor.visualVersion, markReactionSeen, reactionIndex, systemReduced, world.id])
  useEffect(() => { if (selected) panelRef.current?.focus() }, [selected])

  const chooseLocation = (location: SceneLocation) => {
    setSelectedId(location.id); inspectLocation(world.id, location.id, descriptor.visualVersion)
    if (onSelectTeam && location.teamId) onSelectTeam(location.teamId)
  }
  const closeReaction = () => { markReactionSeen(world.id, descriptor.visualVersion); setReactionIndex(-1) }
  const skipIntro = () => { completeIntroduction(world.id); setIntroIndex(-1) }
  const toggleAudio = async () => {
    const enabled = !audioEnabled; setAudioEnabled(enabled)
    if (!enabled) marketAudio.stop(); else await marketAudio.start(descriptor.soundscape)
  }
  return <section className={`market-scene-frame${display ? ' market-scene-frame--display' : ''}`} aria-label="جهان دیداری بازار هزارچراغ" data-scene-id={descriptor.id} data-scene-version={descriptor.visualVersion}>
    <div className="scene-toolbar">
      <div><span className="eyebrow">{descriptor.timeLabel} · {descriptor.atmosphereLabel}</span><strong>{descriptor.title}</strong>{descriptor.publicEvent && <small>{descriptor.publicEvent}</small>}</div>
      <div className="scene-controls" aria-label="تنظیمات نمای بازار">
        {!display && <label><span className="sr-only">کیفیت نمای بازار</span><select value={preferredMode} onChange={(event) => { setPixiFailed(false); setVisualMode(event.target.value as 'high' | 'reduced' | 'fallback') }}><option value="high">نمای پُرجزئیات</option><option value="reduced">نمای کم‌تحرک</option><option value="fallback">نقشه ساده</option></select></label>}
        <button className="scene-control" type="button" aria-pressed={audioEnabled} onClick={() => void toggleAudio()}>{audioEnabled ? 'قطع صدای محیط' : 'صدای محیط'}</button>
      </div>
    </div>
    <div className={`market-viewport visual-${mode}${introLocation ? ' is-guided' : ''}`} style={style}>
      {mode === 'high' ? <Suspense fallback={<FallbackMarket descriptor={visibleDescriptor} reduced />}><PixiMarketCanvas descriptor={visibleDescriptor} onFailure={onFailure} /></Suspense> : <FallbackMarket descriptor={visibleDescriptor} reduced={mode === 'reduced'} />}
      {!display && descriptor.semantic && <div className="market-hotspots" aria-label="نقاط قابل بررسی بازار">{descriptor.locations.map((location) => {
        const isNew = inspected[`${world.id}:${location.id}`] !== descriptor.visualVersion && location.state !== 'quiet'
        return <button type="button" key={location.id} className={`market-hotspot state-${location.state}${isNew ? ' is-new' : ''}`} style={{ insetInlineStart: `${location.x / 10}%`, top: `${location.y / 7}%` }} aria-label={`${location.name}؛ ${location.currentCondition}`} aria-pressed={selectedId === location.id} onClick={() => chooseLocation(location)}><span aria-hidden="true" />{location.name}</button>
      })}</div>}
      {introLocation && <div className="guided-intro" role="dialog" aria-label="معرفی کوتاه بازار"><span>{introIndex + 1} / {introIds.length}</span><strong>{introLocation.name}</strong><p>{introLocation.shortIdentity}</p>{systemReduced && <ol>{introIds.map((id) => <li key={id}>{descriptor.locations.find((location) => location.id === id)?.name}</li>)}</ol>}<button type="button" onClick={skipIntro}>{systemReduced ? 'پایان معرفی' : 'رد کردن معرفی'}</button></div>}
      {reactionIndex >= 0 && descriptor.reactions[reactionIndex] && <div className="world-reaction" role="status" aria-live="assertive"><span>چه چیزی عوض شد؟</span><p>{descriptor.reactions[reactionIndex].outcomeLine}</p><button type="button" onClick={closeReaction}>ادامه روایت</button></div>}
      {pixiFailed && <p className="scene-fallback-notice" role="status">نمای ساده فعال شد؛ روایت، انتخاب‌ها و تعامل‌ها بدون وقفه ادامه دارند.</p>}
    </div>
    {selected && <aside className="location-context" ref={panelRef} tabIndex={-1} aria-label={`اطلاعات ${selected.name}`}><button className="context-close" type="button" onClick={() => setSelectedId(undefined)}>بستن</button><p className="eyebrow">{selected.state === 'changed-since-last-visit' ? 'تغییر تازه' : 'اکنون در بازار'}</p><h3>{selected.name}</h3><p>{selected.shortIdentity}</p><p><strong>چرا مهم است؟</strong> {selected.whyItMatters}</p><p><strong>اکنون:</strong> {selected.currentCondition}</p>{selected.whoIsHere && <p><strong>حاضر:</strong> {selected.whoIsHere}</p>}{selected.recentChange && <p><strong>تغییر اخیر:</strong> {selected.recentChange}</p>}{descriptor.characters.filter((character) => character.locationId === selected.id).map((character) => <details key={character.id}><summary>{character.displayName} · {character.attitude}</summary><p>{character.whatIsKnown}</p><p>{character.recentStatement}</p></details>)}{selected.availableActions.length > 0 && <ul>{selected.availableActions.map((action) => <li key={action}>{action}</li>)}</ul>}</aside>}
    <div className="market-pulse"><div><span className="eyebrow">نبض بازار</span>{descriptor.pulse.length ? <ul>{descriptor.pulse.map((line) => <li key={line}>{line}</li>)}</ul> : <p>{descriptor.atmosphereLabel}</p>}</div>{descriptor.businessPulse.length > 0 && <div><span className="eyebrow">وضعیت حجره شما</span><ul>{descriptor.businessPulse.map((line) => <li key={line}>{line}</li>)}</ul></div>}</div>
    {!display && <details className="location-list"><summary>فهرست دسترس‌پذیر مکان‌ها</summary><ul>{descriptor.locations.map((location) => <li key={location.id}><button type="button" onClick={() => chooseLocation(location)}>{location.name}</button><span>{location.currentCondition}</span></li>)}</ul></details>}
    <div className="sr-only" aria-live="polite">{descriptor.courtyardActivity}{descriptor.ambientEvents.map((event) => ` ${event.description}`)}</div>
    {import.meta.env.DEV && <details className="market-dev-diagnostics"><summary>Market diagnostics</summary><code>canvas={diagnostics.activeCanvasCount} scene={diagnostics.activeSceneId} ambient={diagnostics.ambientEntityCount} handlers={diagnostics.registeredRealtimeHandlers} fallback={String(diagnostics.fallbackState)}</code></details>}
  </section>
}
