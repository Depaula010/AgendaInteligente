import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { Toaster } from 'react-hot-toast'

import { LoginPage } from '@/pages/auth/Login'
import { RegisterPage } from '@/pages/auth/Register'
import { ProtectedRoute } from '@/components/ui/ProtectedRoute'

/**
 * Placeholder para o Dashboard — será implementado nas próximas features.
 */
function DashboardPage() {
  return (
    <div className="min-h-screen bg-surface-900 flex items-center justify-center">
      <p className="text-white text-xl font-semibold">
        🎉 Dashboard em construção!
      </p>
    </div>
  )
}

export function App() {
  return (
    <BrowserRouter>
      {/* Toast global — posicionado no topo centro para mobile */}
      <Toaster
        position="top-center"
        gutter={8}
        toastOptions={{
          duration: 4000,
          style: {
            background: '#1e293b',
            color: '#f1f5f9',
            border: '1px solid rgba(255,255,255,0.1)',
            borderRadius: '12px',
            fontSize: '14px',
            fontFamily: 'Inter, sans-serif',
            padding: '12px 16px',
          },
          success: {
            iconTheme: { primary: '#0ea5e9', secondary: '#0f172a' },
          },
          error: {
            iconTheme: { primary: '#f87171', secondary: '#0f172a' },
          },
        }}
      />

      <Routes>
        {/* Rotas públicas */}
        <Route path="/login" element={<LoginPage />} />
        <Route path="/cadastro" element={<RegisterPage />} />

        {/* Rotas protegidas */}
        <Route element={<ProtectedRoute />}>
          <Route path="/dashboard" element={<DashboardPage />} />
        </Route>

        {/* Fallback */}
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
