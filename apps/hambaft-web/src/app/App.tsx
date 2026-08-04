import { Navigate, Route, Routes } from 'react-router-dom'
import { HomePage } from '../routes/HomePage'
import { NotFoundPage } from '../routes/NotFoundPage'
import { TeamGuard, AdminGuard } from '../routes/guards'
import { PairingPage } from '../features/pairing/PairingPage'
import { TeamShell } from '../features/team-experience/TeamShell'
import { StoryPage } from '../features/narrative/StoryPage'
import { MarketPage } from '../features/public-display/MarketPage'
import { MessagesPage } from '../features/proposals/MessagesPage'
import { AgreementsPage } from '../features/agreements/AgreementsPage'
import { StatusPage } from '../features/team-experience/StatusPage'
import { EndingPage } from '../features/endings/EndingPage'
import { AdminHomePage } from '../features/admin/AdminHomePage'
import { AdminLayout } from '../features/admin/AdminLayout'
import { SetupPage } from '../features/admin/SetupPage'
import { RuntimePage } from '../features/admin/RuntimePage'
import { PublicDisplayPage } from '../features/public-display/PublicDisplayPage'

export function App() {
  return <Routes>
    <Route path="/" element={<HomePage />} />
    <Route path="/pair" element={<PairingPage />} />
    <Route element={<TeamGuard />}><Route path="/team" element={<TeamShell />}><Route index element={<Navigate to="story" replace />} /><Route path="story" element={<StoryPage />} /><Route path="market" element={<MarketPage />} /><Route path="messages" element={<MessagesPage />} /><Route path="agreements" element={<AgreementsPage />} /><Route path="status" element={<StatusPage />} /><Route path="ending" element={<EndingPage />} /></Route></Route>
    <Route path="/display/:sessionId" element={<PublicDisplayPage />} />
    <Route path="/admin" element={<AdminHomePage />} />
    <Route element={<AdminGuard />}><Route path="/admin/session/:sessionId" element={<AdminLayout />}><Route index element={<Navigate to="setup" replace />} /><Route path="setup" element={<SetupPage />} /><Route path="runtime" element={<RuntimePage />} /></Route></Route>
    <Route path="*" element={<NotFoundPage />} />
  </Routes>
}
