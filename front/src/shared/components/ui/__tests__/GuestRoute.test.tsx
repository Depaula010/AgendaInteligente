import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { GuestRoute } from '@/shared/components/ui/GuestRoute'
import { useAuthStore } from '@/features/auth/store/authStore'

vi.mock('@/features/auth/store/authStore')

type AuthSlice = { isAuthenticated: boolean; _hasHydrated: boolean }

function mockAuth({ isAuthenticated, _hasHydrated }: AuthSlice) {
  vi.mocked(useAuthStore).mockImplementation((selector: (s: AuthSlice) => unknown) =>
    selector({ isAuthenticated, _hasHydrated }),
  )
}

function renderRoute(initialPath = '/login') {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        <Route element={<GuestRoute />}>
          <Route path="/login" element={<div>Página de login</div>} />
        </Route>
        <Route path="/dashboard" element={<div>Dashboard</div>} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('GuestRoute', () => {
  it('shows loading screen before store hydration', () => {
    mockAuth({ isAuthenticated: false, _hasHydrated: false })
    renderRoute()
    expect(screen.getByRole('status', { name: /carregando/i })).toBeInTheDocument()
  })

  it('renders outlet when unauthenticated and hydrated', () => {
    mockAuth({ isAuthenticated: false, _hasHydrated: true })
    renderRoute()
    expect(screen.getByText('Página de login')).toBeInTheDocument()
  })

  it('redirects to /dashboard when authenticated and hydrated', () => {
    mockAuth({ isAuthenticated: true, _hasHydrated: true })
    renderRoute()
    expect(screen.getByText('Dashboard')).toBeInTheDocument()
    expect(screen.queryByText('Página de login')).not.toBeInTheDocument()
  })
})
