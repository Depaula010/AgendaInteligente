import { lazy, Suspense } from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { ROUTES } from '@/app/routes'
import { ProtectedRoute } from '@/shared/components/ui/ProtectedRoute'
import { GuestRoute } from '@/shared/components/ui/GuestRoute'
import { LoadingScreen } from '@/shared/components/ui/LoadingScreen'
import { DashboardLayout } from '@/shared/components/layouts/DashboardLayout'

// Lazy loading — cada página é um chunk separado
const LoginPage = lazy(() =>
  import('@/features/auth/pages/Login').then((m) => ({ default: m.LoginPage })),
)
const ForgotPasswordPage = lazy(() =>
  import('@/features/auth/pages/ForgotPassword').then((m) => ({ default: m.ForgotPasswordPage })),
)
const ResetPasswordPage = lazy(() =>
  import('@/features/auth/pages/ResetPassword').then((m) => ({ default: m.ResetPasswordPage })),
)
const OnboardingPage = lazy(() =>
  import('@/features/onboarding/pages/OnboardingPage').then((m) => ({
    default: m.OnboardingPage,
  })),
)
const AgendaPage = lazy(() =>
  import('@/features/agenda/pages/AgendaPage').then((m) => ({ default: m.AgendaPage })),
)
const WhatsAppPage = lazy(() =>
  import('@/features/whatsapp/pages/WhatsAppPage').then((m) => ({ default: m.WhatsAppPage })),
)


const ClientesPage = lazy(() =>
  import('@/features/clientes/pages/ClientesPage').then((m) => ({ default: m.ClientesPage })),
)
const EquipePage = lazy(() =>
  import('@/features/equipe/pages/EquipePage').then((m) => ({ default: m.EquipePage })),
)
const ServicosPage = lazy(() =>
  import('@/features/servicos/pages/ServicosPage').then((m) => ({ default: m.ServicosPage })),
)
const ConfiguracoesPage = lazy(() =>
  import('@/features/configuracoes/pages/ConfiguracoesPage').then((m) => ({
    default: m.ConfiguracoesPage,
  })),
)

export function AppRouter() {
  return (
    <Suspense fallback={<LoadingScreen />}>
      <Routes>
        {/* Rotas públicas acessíveis sem autenticação (não redirecionam autenticados) */}
        <Route path={ROUTES.FORGOT_PASSWORD} element={<ForgotPasswordPage />} />
        <Route path={ROUTES.RESET_PASSWORD} element={<ResetPasswordPage />} />

        {/* Rotas de visitante — redirecionam para /dashboard se autenticado */}
        <Route element={<GuestRoute />}>
          <Route path={ROUTES.LOGIN} element={<LoginPage />} />
          <Route path={ROUTES.REGISTER} element={<OnboardingPage />} />
        </Route>

        {/* Rotas protegidas com DashboardLayout */}
        <Route element={<ProtectedRoute />}>
          <Route element={<DashboardLayout />}>
            <Route
              path={ROUTES.DASHBOARD}
              element={<Navigate to={ROUTES.AGENDA} replace />}
            />
            <Route path={ROUTES.AGENDA} element={<AgendaPage />} />
            <Route path={ROUTES.WHATSAPP} element={<WhatsAppPage />} />
            <Route path={ROUTES.CLIENTES} element={<ClientesPage />} />
            <Route path={ROUTES.EQUIPE} element={<EquipePage />} />
            <Route path={ROUTES.SERVICOS} element={<ServicosPage />} />
            <Route path={ROUTES.CONFIGURACOES} element={<ConfiguracoesPage />} />
          </Route>
        </Route>

        {/* Fallback */}
        <Route path="*" element={<Navigate to={ROUTES.LOGIN} replace />} />
      </Routes>
    </Suspense>
  )
}
