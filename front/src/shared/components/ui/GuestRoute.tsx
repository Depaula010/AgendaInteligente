import { Navigate, Outlet } from 'react-router-dom'
import { useAuthStore } from '@/features/auth/store/authStore'
import { LoadingScreen } from '@/shared/components/ui/LoadingScreen'
import { ROUTES } from '@/app/routes'

/**
 * GuestRoute — protege rotas de autenticação (login, cadastro).
 * Se o store ainda está rehidratando, exibe LoadingScreen.
 * Se o usuário já está autenticado, redireciona para o dashboard.
 */
export function GuestRoute() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const hasHydrated = useAuthStore((s) => s._hasHydrated)

  if (!hasHydrated) return <LoadingScreen />
  return isAuthenticated ? <Navigate to={ROUTES.DASHBOARD} replace /> : <Outlet />
}