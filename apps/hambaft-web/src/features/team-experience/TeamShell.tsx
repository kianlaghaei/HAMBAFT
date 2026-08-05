import { Outlet, useNavigate } from 'react-router-dom'
import { RealtimeBoundary, ConnectionBadge, useConnectionStatus } from '../../realtime/RealtimeBoundary'
import { useAuthStore } from '../../auth/authStore'
import { useTeamExperience } from '../../hooks/useServerQuery'
import { checkpointLabel, sessionStatusName } from '../../design-system/presentation'
import { ErrorState, LoadingState } from '../../components/States'

function ShellContent() {
  const navigate = useNavigate()
  const unpair = useAuthStore((state) => state.unpair)
  const experience = useTeamExperience()
  const connection = useConnectionStatus()
  if (experience.isLoading) return <LoadingState />
  if (experience.isError || !experience.data) return <ErrorState error={experience.error} retry={() => void experience.refetch()} />
  const { controlledEntity, currentCheckpointId } = experience.data
  const isGameplay = experience.data.sessionMetadata?.storyPackageId === 'hezar-cheragh'
  const canWrite = connection === 'connected' && sessionStatusName(experience.data.sessionMetadata?.status ?? '') === 'Running'
  return <div className={`app-shell${isGameplay ? ' app-shell--gameplay' : ''}`}>
    {!isGameplay && <>
      <header className="team-header">
        <div><p className="brand">HAMBAFT <span>/ هزارچراغ</span></p><h1>{controlledEntity?.displayName ?? experience.data.team.displayName}</h1></div>
        <div className="header-meta"><ConnectionBadge /><span className="atmosphere">{checkpointLabel(currentCheckpointId)}</span><button className="text-button" onClick={() => { unpair(); navigate('/pair', { replace: true }) }}>خروج از جلسه</button></div>
      </header>
      {connection !== 'connected' && <div className="connection-banner" role="alert">وضعیت خوانده‌شده حفظ شده است؛ تا برقراری دوباره ارتباط، اقدام‌های تغییردهنده غیرفعال‌اند.</div>}
      {sessionStatusName(experience.data.sessionMetadata?.status ?? '') === 'Paused' && <div className="pause-banner">جلسه موقتاً توسط راهبر متوقف شده است.</div>}
    </>}
    <main className={`team-main${isGameplay ? ' team-main--gameplay' : ''}`}><Outlet context={{ experience: experience.data, canWrite }} /></main>
  </div>
}

export function TeamShell() {
  const team = useAuthStore((state) => state.team)
  return <RealtimeBoundary token={team?.accessToken} sessionId={team?.sessionId}><ShellContent /></RealtimeBoundary>
}
