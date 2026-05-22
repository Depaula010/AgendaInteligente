import { BrowserRouter } from 'react-router-dom'
import { Toaster } from 'react-hot-toast'
import { AppRouter } from '@/app/router'
import { OfflineBanner } from '@/shared/components/ui/OfflineBanner'

export function App() {
  return (
    <BrowserRouter>
      <Toaster
        position="top-center"
        gutter={8}
        toastOptions={{
          duration: 4000,
          style: {
            background: 'var(--toast-bg)',
            color: 'var(--toast-text)',
            border: '1px solid var(--toast-border)',
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
      <AppRouter />
      <OfflineBanner />
    </BrowserRouter>
  )
}
