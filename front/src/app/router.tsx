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

// Placeholder para rotas ainda não implementadas
function ComingSoon({ label }: { label: string }) {
  return (
    <div className="flex flex-col items-center justify-center h-full p-8 text-center">
      <p className="text-2xl mb-2">🚧</p>
      <p className="text-white font-semibold">{label}</p>
      <p className="text-sm text-slate-500 mt-1">Em breve</p>
    </div>
  )
}

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
  Promise.resolve({ default: () => <ComingSoon label="Configurações" /> }),
)

export function AppRouter() {
  return (
    <Suspense fallback={<LoadingScreen />}>
      <Routes>
        {/* Rotas públicas — redirecionam para /dashboard se autenticado */}
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
