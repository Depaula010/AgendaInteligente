import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from '@/shared/components/ui/ProtectedRoute'
import { useAuthStore } from '@/features/auth/store/authStore'

vi.mock('@/features/auth/store/authStore')

type AuthSlice = { isAuthenticated: boolean; _hasHydrated: boolean }

function mockAuth({ isAuthenticated, _hasHydrated }: AuthSlice) {
  vi.mocked(useAuthStore).mockImplementation((selector: (s: AuthSlice) => unknown) =>
    selector({ isAuthenticated, _hasHydrated }),
  )
}

function renderRoute(initialPath = '/protected') {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        <Route element={<ProtectedRoute />}>
          <Route path="/protected" element={<div>Conteúdo protegido</div>} />
        </Route>
        <Route path="/login" element={<div>Página de login</div>} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('ProtectedRoute', () => {
  it('shows loading screen before store hydration', () => {
    mockAuth({ isAuthenticated: false, _hasHydrated: false })
    renderRoute()
    expect(screen.getByRole('status', { name: /carregando/i })).toBeInTheDocument()
  })

  it('renders outlet when authenticated and hydrated', () => {
    mockAuth({ isAuthenticated: true, _hasHydrated: true })
    renderRoute()
    expect(screen.getByText('Conteúdo protegido')).toBeInTheDocument()
  })

  it('redirects to /login when unauthenticated and hydrated', () => {
    mockAuth({ isAuthenticated: false, _hasHydrated: true })
    renderRoute()
    expect(screen.getByText('Página de login')).toBeInTheDocument()
    expect(screen.queryByText('Conteúdo protegido')).not.toBeInTheDocument()
  })
})
