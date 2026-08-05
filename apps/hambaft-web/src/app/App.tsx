import { useEffect } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { HomePage } from '../routes/HomePage'
import { NotFoundPage } from '../routes/NotFoundPage'
import { TeamGuard, AdminEntryGuard, AdminGuard } from '../routes/guards'
import { PairingPage } from '../features/pairing/PairingPage'
import { TeamShell } from '../features/team-experience/TeamShell'
import { StoryPage } from '../features/narrative/StoryPage'
import { AdminHomePage } from '../features/admin/AdminHomePage'
import { AdminLayout } from '../features/admin/AdminLayout'
import { SetupPage } from '../features/admin/SetupPage'
import { RuntimePage } from '../features/admin/RuntimePage'
import { PublicDisplayPage } from '../features/public-display/PublicDisplayPage'
import { DevVisualLab } from '../features/hezar-cheragh/DevVisualLab'
import { useUiStore } from '../state/uiStore'
import { isCredentialValid, useAuthStore } from '../auth/authStore'

function HomeEntry() {
  const team = useAuthStore((state) => state.team)
  return isCredentialValid(team) ? <Navigate to="/team" replace /> : <HomePage />
}

export function App() {
  const setReducedMotion = useUiStore((state) => state.setReducedMotion)
  useEffect(() => {
    if (!window.matchMedia) return
    const media = window.matchMedia('(prefers-reduced-motion: reduce)')
    const sync = () => setReducedMotion(media.matches)
    sync()
    media.addEventListener('change', sync)
    return () => media.removeEventListener('change', sync)
  }, [setReducedMotion])
  return <Routes>
    <Route path="/" element={<HomeEntry />} />
    {import.meta.env.DEV && <Route path="/dev/hezar-cheragh-ui" element={<DevVisualLab />} />}
    <Route path="/pair" element={<PairingPage />} />
    <Route element={<TeamGuard />}>
      <Route path="/team" element={<TeamShell />}>
        <Route index element={<StoryPage />} />
      </Route>
      <Route path="/team/story" element={<Navigate to="/team" replace />} />
      <Route path="/team/market" element={<Navigate to="/team" replace />} />
      <Route path="/team/messages" element={<Navigate to="/team" replace />} />
      <Route path="/team/agreements" element={<Navigate to="/team" replace />} />
      <Route path="/team/status" element={<Navigate to="/team" replace />} />
      <Route path="/team/ending" element={<Navigate to="/team" replace />} />
    </Route>
    <Route path="/display/:sessionId" element={<PublicDisplayPage />} />
    <Route element={<AdminEntryGuard />}><Route path="/admin" element={<AdminHomePage />} /></Route>
    <Route element={<AdminGuard />}><Route path="/admin/session/:sessionId" element={<AdminLayout />}><Route index element={<Navigate to="setup" replace />} /><Route path="setup" element={<SetupPage />} /><Route path="runtime" element={<RuntimePage />} /></Route></Route>
    <Route path="*" element={<NotFoundPage />} />
  </Routes>
}
