import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { isCredentialValid, useAuthStore } from '../auth/authStore'

export function TeamGuard() {
  const team = useAuthStore((state) => state.team)
  const location = useLocation()
  return isCredentialValid(team) ? <Outlet /> : <Navigate to="/pair" replace state={{ from: location.pathname }} />
}

export function AdminGuard() {
  const admin = useAuthStore((state) => state.admin)
  return isCredentialValid(admin) ? <Outlet /> : <Navigate to="/admin" replace />
}
