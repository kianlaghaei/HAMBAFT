import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { api } from '../../api/client'
import { queryKeys } from '../../api/queryKeys'
import { useAuthStore } from '../../auth/authStore'
import { ErrorState, LoadingState, Section } from '../../components/States'
import { controllerLabel, statusLabel } from '../../design-system/presentation'
import { useUiStore } from '../../state/uiStore'

export function SetupPage() {
  const { sessionId = '' } = useParams()
  const token = useAuthStore((state) => state.admin?.accessToken ?? '')
  const codes = useUiStore((state) => state.setupCodes[sessionId] ?? [])
  const client = useQueryClient()
  const session = useQuery({ queryKey: queryKeys.session(sessionId), queryFn: () => api.session(sessionId) })
  const action = useMutation({ mutationFn: async (name: 'start' | 'initialize') => {
    if (!session.data) throw new Error('Session unavailable')
    return name === 'start' ? api.transition(sessionId, 'start', session.data.stateVersion) : api.initialize(sessionId, session.data.stateVersion, token)
  }, onSuccess: () => void client.invalidateQueries({ queryKey: queryKeys.session(sessionId) }) })
  if (session.isLoading) return <LoadingState />
  if (session.isError || !session.data) return <ErrorState error={session.error} retry={() => void session.refetch()} />
  const data = session.data
  return <div className="content-grid"><Section title="قفل محتوای جلسه" eyebrow={statusLabel(data.status)}><dl className="detail-list"><div><dt>بسته</dt><dd>{data.storyPackageId} / {data.storyVersion}</dd></div><div><dt>Content hash</dt><dd dir="ltr" className="hash">{data.contentHash}</dd></div><div><dt>نسخه وضعیت</dt><dd>{data.stateVersion}</dd></div></dl></Section>
    <Section title="تیم‌ها و تخصیص‌ها" eyebrow="کنترل‌شده و رفتار تألیفی"><div className="setup-entities">{data.entities.map((entity) => <article key={entity.id}><h3>{entity.displayName}</h3><p>{controllerLabel(entity.controllerType)}</p><span className="status-chip">{entity.controlledByTeamId ? data.teams.find((team) => team.id === entity.controlledByTeamId)?.displayName : entity.behaviorProfileId}</span></article>)}</div></Section>
    <Section title="برگه کدهای جفت‌شدن" eyebrow="فقط نمایش یک‌باره" actions={<div className="button-row print-hidden"><button className="button button--ghost" onClick={() => window.print()}>چاپ</button><button className="button button--ghost" onClick={() => void navigator.clipboard.writeText(codes.map((item) => `${item.teamName} — ${item.businessName}: ${item.code}`).join('\n'))}>کپی همه</button></div>}>{codes.length ? <div className="pairing-slips">{codes.map((item) => <article key={item.teamId}><span>{item.businessName}</span><h3>{item.teamName}</h3><code>{item.code}</code></article>)}</div> : <p className="form-help">کدهای خام از Backend دوباره قابل دریافت نیستند. این مرورگر آن‌ها را فقط اگر جلسه را همین‌جا ساخته باشد، در sessionStorage نگه می‌دارد.</p>}</Section>
    <Section title="آماده‌سازی روایت" eyebrow="اقدام‌های راهبر"><div className="button-row"><button className="button" disabled={data.status !== 'Lobby' || action.isPending} onClick={() => action.mutate('start')}>شروع جلسه</button><button className="button" disabled={data.status !== 'Running' || action.isPending} onClick={() => action.mutate('initialize')}>مقداردهی روایت</button></div>{action.error && <ErrorState error={action.error} />}</Section>
  </div>
}
