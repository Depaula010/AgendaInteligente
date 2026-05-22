import { Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from '@/features/auth/pages/Login'
import { RegisterPage } from '@/features/auth/pages/Register'
import { ProtectedRoute } from '@/shared/components/ui/ProtectedRoute'

function DashboardPage() {
  return (
    <div className="min-h-screen bg-surface-900 flex items-center justify-center">
      <p className="text-white text-xl font-semibold">Dashboard em construção!</p>
    </div>
  )
}

export function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/cadastro" element={<RegisterPage />} />

      <Route element={<ProtectedRoute />}>
        <Route path="/dashboard" element={<DashboardPage />} />
      </Route>

      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  )
}
