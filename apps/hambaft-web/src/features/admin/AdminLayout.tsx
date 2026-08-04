import { NavLink, Outlet, useParams } from 'react-router-dom'
import { ConnectionBadge, RealtimeBoundary } from '../../realtime/RealtimeBoundary'
import { useAuthStore } from '../../auth/authStore'

export function AdminLayout() {
  const { sessionId } = useParams()
  const admin = useAuthStore((state) => state.admin)
  const clear = useAuthStore((state) => state.clearAdmin)
  return <RealtimeBoundary token={admin?.accessToken} sessionId={sessionId}><div className="admin-shell"><header><div><p className="brand">HAMBAFT <span>/ میز راهبر</span></p><h1>کنترل جلسه</h1></div><div className="header-meta"><ConnectionBadge /><button className="text-button" onClick={clear}>خروج Admin</button></div></header><nav>{sessionId && <><NavLink to={`/admin/session/${sessionId}/setup`}>آماده‌سازی</NavLink><NavLink to={`/admin/session/${sessionId}/runtime`}>اجرای زنده</NavLink><NavLink to={`/display/${sessionId}`} target="_blank">نمایش عمومی</NavLink></>}</nav><main><Outlet /></main></div></RealtimeBoundary>
}
