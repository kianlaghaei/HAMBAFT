import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { api } from '../../api/client'
import { queryKeys } from '../../api/queryKeys'
import { useAuthStore } from '../../auth/authStore'
import { EmptyState, ErrorState, LoadingState, Section } from '../../components/States'
import { checkpointLabel, faDigits, metricLabel, statusLabel } from '../../design-system/presentation'
import { useConnectionStatus } from '../../realtime/RealtimeBoundary'

export function RuntimePage() {
  const { sessionId = '' } = useParams()
  const token = useAuthStore((state) => state.admin?.accessToken ?? '')
  const connection = useConnectionStatus()
  const client = useQueryClient()
  const session = useQuery({ queryKey: queryKeys.session(sessionId), queryFn: () => api.session(sessionId), refetchInterval: connection === 'disconnected' ? 10_000 : false })
  const admin = useQuery({ queryKey: queryKeys.admin(sessionId), queryFn: () => api.admin(sessionId, token) })
  const mutation = useMutation({ mutationFn: async (action: 'pause' | 'resume' | 'resolve' | 'endings' | 'initialize') => {
    if (!session.data || !admin.data) throw new Error('Session unavailable')
    if (action === 'resolve') return api.resolveCheckpoint(sessionId, admin.data.streamVersion, token)
    if (action === 'endings') return api.resolveEndings(sessionId, admin.data.streamVersion, token)
    if (action === 'initialize') return api.initialize(sessionId, admin.data.streamVersion, token)
    return api.transition(sessionId, action, session.data.stateVersion)
  }, onSuccess: () => { void client.invalidateQueries({ queryKey: queryKeys.session(sessionId) }); void client.invalidateQueries({ queryKey: queryKeys.admin(sessionId) }) } })
  if (session.isLoading || admin.isLoading) return <LoadingState />
  if (session.isError || admin.isError || !session.data || !admin.data) return <ErrorState error={session.error ?? admin.error} retry={() => { void session.refetch(); void admin.refetch() }} />
  const s = session.data; const a = admin.data
  const canResolve = s.status === 'Running' && a.teamsAwaitingResponse.length === 0
  const likelyEndingReady = canResolve && a.openProposals.length === 0 && a.pendingConsequences.length === 0 && a.assignedStoryletIds.length === 0 && a.endingEligibilityDiagnostics.some((item) => item.eligible)
  return <div className="content-grid"><Section title={checkpointLabel(a.checkpoint)} eyebrow={`جلسه ${statusLabel(s.status)}`}><div className="runtime-stats"><article><strong>{faDigits(s.teams.length)}</strong><span>تیم</span></article><article><strong>{faDigits(a.teamsAwaitingResponse.length)}</strong><span>منتظر پاسخ</span></article><article><strong>{faDigits(a.openProposals.length)}</strong><span>پیشنهاد باز</span></article><article><strong>{faDigits(a.activeAgreements.length)}</strong><span>پیمان فعال</span></article><article><strong>{faDigits(a.pendingConsequences.length)}</strong><span>پیامد در انتظار</span></article><article><strong>{faDigits(a.streamVersion)}</strong><span>نسخه جریان</span></article></div></Section>
    {a.marketPreview && <Section title="پیش‌نمایش زنده بازار" eyebrow={`${a.marketPreview.timeLabel} · ${a.marketPreview.atmosphereLabel}`}><div className="admin-market-preview"><div className={`preview-sky time-${a.marketPreview.timeOfDay}`}><span>{a.marketPreview.publicEvent}</span></div><ul>{a.marketPreview.pulse.map((line) => <li key={line}>{line}</li>)}</ul></div></Section>}
    <Section title="اقدام‌های اجرا" eyebrow="Backend اعتبار نهایی را تعیین می‌کند"><div className="button-row"><button className="button button--ghost" disabled={s.status !== 'Running' || mutation.isPending} onClick={() => mutation.mutate('pause')}>توقف موقت</button><button className="button button--ghost" disabled={s.status !== 'Paused' || mutation.isPending} onClick={() => mutation.mutate('resume')}>ادامه</button><button className="button" disabled={!canResolve || mutation.isPending} onClick={() => mutation.mutate('resolve')}>حل ایستگاه روایت</button><button className="button button--gold" disabled={!likelyEndingReady || mutation.isPending} onClick={() => mutation.mutate('endings')}>حل سرانجام‌ها</button></div>{mutation.error && <ErrorState error={mutation.error} />}</Section>
    <Section title="صف پاسخ‌ها" eyebrow="تیم‌های منتظر">{a.teamsAwaitingResponse.length ? <ul className="ledger-list">{a.teamsAwaitingResponse.map((id) => <li key={id}>{s.teams.find((team) => team.id === id)?.displayName ?? 'تیم'}</li>)}</ul> : <EmptyState title="همه پاسخ‌های لازم ثبت شده‌اند" />}</Section>
    <Section title="رفتار کسب‌وکارهای بدون تیم" eyebrow="انتخاب‌های تألیفی ثبت‌شده">{a.uncontrolledBehaviorSelections.length ? <ul className="ledger-list">{a.uncontrolledBehaviorSelections.map((item, index) => <li key={`${item.entityId}-${index}`}>{s.entities.find((entity) => entity.id === item.entityId)?.displayName}: {item.actionId}</li>)}</ul> : <EmptyState title="هنوز رفتاری حل نشده است" />}</Section>
    <Section title="تشخیص آمادگی پایان" eyebrow="شواهد Admin"><div className="diagnostic-grid">{a.endingEligibilityDiagnostics.map((item) => <span className={item.eligible ? 'eligible' : ''} key={item.endingDefinitionId}>{item.endingDefinitionId} — {item.eligible ? 'واجد شرایط' : 'نامعتبر'}</span>)}</div></Section>
    <Section title="تشخیص فنی" eyebrow="فقط برای راهبر"><details className="technical-diagnostics"><summary>نمایش مقادیر خام Backend</summary><p>این بخش برای اجرای عادی بازی لازم نیست.</p><div className="metrics-grid">{a.technicalMetrics?.map((metric) => <article className="metric-card" key={`${metric.scope}-${metric.scopeId}-${metric.metricKey}`}><span>{metricLabel(metric.metricKey)}</span><strong>{faDigits(metric.numericValue)}</strong></article>)}</div></details></Section>
  </div>
}
