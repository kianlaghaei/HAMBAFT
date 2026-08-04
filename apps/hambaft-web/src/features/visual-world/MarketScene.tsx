
import { lazy, Suspense, useCallback, useEffect, useMemo, useState } from 'react'
import type { PublicWorld, TeamExperience } from '../../api/schemas'
import { useUiStore } from '../../state/uiStore'
import { marketAudio } from './audio'
import { FallbackMarket } from './FallbackMarket'
import { buildSceneDescriptor } from './sceneDirector'

const PixiMarketCanvas = lazy(() => import('./PixiMarketCanvas').then((module) => ({ default: module.PixiMarketCanvas })))

export function MarketScene({ world, team, display = false }: { world: PublicWorld; team?: TeamExperience; display?: boolean }) {
  const preferredMode = useUiStore((state) => state.visualMode)
  const setVisualMode = useUiStore((state) => state.setVisualMode)
  const audioEnabled = useUiStore((state) => state.audioEnabled)
  const setAudioEnabled = useUiStore((state) => state.setAudioEnabled)
  const systemReduced = useUiStore((state) => state.reducedMotion)
  const [pixiFailed, setPixiFailed] = useState(false)
  const descriptor = useMemo(() => buildSceneDescriptor(world, team), [team, world])
  const mode = systemReduced && preferredMode === 'high' ? 'reduced' : pixiFailed ? 'fallback' : preferredMode
  const onFailure = useCallback(() => setPixiFailed(true), [])
  useEffect(() => () => marketAudio.stop(), [])
  const toggleAudio = async () => {
    const enabled = !audioEnabled
    setAudioEnabled(enabled)
    if (!enabled) marketAudio.stop()
    else await marketAudio.start(descriptor.ending !== 'none' ? 'ending-ambience' : descriptor.pressure > 0 ? 'soft-crowd-tension' : 'ambient-market')
  }
  return <section className={`market-scene-frame${display ? ' market-scene-frame--display' : ''}`} aria-label="جهان دیداری بازار هزارچراغ" data-scene-id={descriptor.id} data-scene-version={descriptor.visualVersion}>
    <div className="scene-toolbar">
      <div><span className="eyebrow">بازار زنده</span><strong>{descriptor.title}</strong></div>
      <div className="scene-controls" aria-label="تنظیمات نمای بازار">
        {!display && <label><span className="sr-only">کیفیت نمای بازار</span><select value={preferredMode} onChange={(event) => { setPixiFailed(false); setVisualMode(event.target.value as 'high' | 'reduced' | 'fallback') }}><option value="high">نمای پُرجزئیات</option><option value="reduced">نمای کم‌تحرک</option><option value="fallback">نقشه ساده</option></select></label>}
        <button className="scene-control" type="button" aria-pressed={audioEnabled} onClick={() => void toggleAudio()}>{audioEnabled ? 'قطع صدای محیط' : 'صدای محیط'}</button>
      </div>
    </div>
    <div className={`market-viewport visual-${mode}`}>
      {mode === 'high' ? <Suspense fallback={<FallbackMarket descriptor={descriptor} reduced />}><PixiMarketCanvas descriptor={descriptor} onFailure={onFailure} /></Suspense> : <FallbackMarket descriptor={descriptor} reduced={mode === 'reduced'} />}
      <div className="scene-atmosphere" aria-live="polite">{descriptor.atmosphere.map((value) => <span key={value}>{atmosphereLabel(value)}</span>)}</div>
      {pixiFailed && <p className="scene-fallback-notice" role="status">نمای ساده فعال شد؛ بازی بدون وقفه ادامه دارد.</p>}
    </div>
    <ul className="sr-only" aria-label="حجره‌های حاضر در نقشه">{descriptor.locations.map((location) => <li key={location.entityId}>{location.name}</li>)}</ul>
  </section>
}

function atmosphereLabel(value: string) {
  return ({ quiet: 'سکوت بازار', uncertain: 'ابهام', shortage: 'کمبود بار', rumour: 'شایعه', pressure: 'فشار', watchful: 'نگاه آوان', crowded: 'گردهمایی', resolved: 'شبِ سرانجام', waiting: 'انتظار', tense: 'تنش', steady: 'ثبات', guarded: 'احتیاط' } as Record<string, string>)[value] ?? value
}
