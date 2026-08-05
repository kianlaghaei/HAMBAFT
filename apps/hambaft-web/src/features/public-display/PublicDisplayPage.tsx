import { useEffect } from 'react'
import { useParams } from 'react-router-dom'
import { api } from '../../api/client'
import { useAuthStore } from '../../auth/authStore'
import { ErrorState, LoadingState } from '../../components/States'
import { checkpointLabel, consequenceLabel, controllerLabel, statusLabel } from '../../design-system/presentation'
import { usePackage, usePublicWorld } from '../../hooks/useServerQuery'
import { ConnectionBadge, RealtimeBoundary } from '../../realtime/RealtimeBoundary'
import { EndingView } from '../endings/EndingView'
import { TermsView } from '../../components/TermsView'
import { MarketScene } from '../visual-world/MarketScene'

export function DisplayContent({ sessionId }: { sessionId: string }) {
  const world = usePublicWorld(sessionId)
  const pkg = usePackage(world.data?.storyPackageId, world.data?.storyVersion)
  if (world.isLoading) return <LoadingState label="بازار در حال روشن‌شدن است…" />
  if (world.isError || !world.data) return <ErrorState error={world.error} retry={() => void world.refetch()} />
  const data = world.data
  const publicEntityEndings = (data.publicEntityEndingSummaries ?? []).filter((ending) => ending.title.trim() || ending.paragraphs.length > 0 || ending.evidence.length > 0)
  return <main className="display-page"><header className="display-header"><div><p className="brand">HAMBAFT <span>/ هزارچراغ</span></p><h1>{data.narrative?.title || checkpointLabel(data.currentCheckpointId)}</h1></div><ConnectionBadge /></header>
    {data.storyPackageId === 'hezar-cheragh' && <MarketScene world={data} display />}
    <section className="display-narrative"><p className="eyebrow">{checkpointLabel(data.currentCheckpointId)}</p>{data.narrative?.paragraphs.map((paragraph, index) => <p key={index}>{paragraph}</p>) ?? <p>بازار هنوز منتظر آغاز روایت است.</p>}</section>
    {data.worldPresentation && <section className="display-pulse"><p className="eyebrow">{data.worldPresentation.timeLabel} · {data.worldPresentation.atmosphereLabel}</p><h2>نبض بازار</h2>{data.worldPresentation.pulse.map((line) => <p key={line}>{line}</p>)}{data.worldPresentation.courtyardActivity && <p>{data.worldPresentation.courtyardActivity}</p>}</section>}
    <section className="display-market"><h2>حجره‌های بازار</h2><div className="market-grid">{data.entities.map((entity) => { const semantic = data.worldPresentation?.businesses.find((item) => item.entityId === entity.id); return <article className="business-tile" key={entity.id}><div className="shop-mark">✦</div><h3>{entity.displayName}</h3><p>{semantic?.pulse[0] ?? controllerLabel(entity.controllerType)}</p><span>{statusLabel(entity.status)}</span></article> })}</div></section>
    {(data.worldPresentation?.ambientEvents.length ?? 0) > 0 && <section><h2>آنچه در بازار دیده می‌شود</h2><ul className="ledger-list">{data.worldPresentation?.ambientEvents.map((event) => <li key={event.id}>{event.description}</li>)}</ul></section>}
    {(data.publicAgreements?.length ?? 0) > 0 && <section><h2>پیمان‌های آشکار</h2><div className="agreement-grid">{data.publicAgreements?.map((agreement) => <article className="agreement-card" key={agreement.agreementId}><h3>{pkg.data?.interactions.find((item) => item.id === agreement.interactionTypeId)?.displayName ?? 'پیمان بازار'}</h3><TermsView terms={agreement.termsPayload} /><span>{statusLabel(agreement.status)}</span></article>)}</div></section>}
    {(data.publicConsequences?.length ?? 0) > 0 && <section><h2>پیامدهای مشترک</h2><ul className="ledger-list">{data.publicConsequences?.map((item) => <li key={item.scheduledConsequenceId}>{consequenceLabel(item.definitionId)}</li>)}</ul></section>}
    {data.worldEnding && <section className="display-ending"><EndingView ending={data.worldEnding} world />{publicEntityEndings.map((ending) => <EndingView ending={ending} key={ending.endingResultId} />)}</section>}
  </main>
}

export function PublicDisplayPage() {
  const { sessionId = '' } = useParams()
  const credential = useAuthStore((state) => state.displays[sessionId])
  const setToken = useAuthStore((state) => state.setToken)
  useEffect(() => {
    if (!credential && sessionId) api.devToken('public-display', sessionId).then(setToken).catch(() => undefined)
  }, [credential, sessionId, setToken])
  return <RealtimeBoundary token={credential?.accessToken} sessionId={sessionId}><DisplayContent sessionId={sessionId} /></RealtimeBoundary>
}
