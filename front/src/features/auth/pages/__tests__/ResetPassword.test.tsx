import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { ResetPasswordPage } from '@/features/auth/pages/ResetPassword'
import { authService } from '@/features/auth/services/auth.service'

vi.mock('@/features/auth/services/auth.service')
vi.mock('react-hot-toast', () => ({
  default: {
    loading: vi.fn(() => 'toast-id'),
    success: vi.fn(),
    error:   vi.fn(),
  },
}))

function renderPage(search = '?token=valid-token-abc') {
  return render(
    <MemoryRouter initialEntries={[`/redefinir-senha${search}`]}>
      <Routes>
        <Route path="/redefinir-senha" element={<ResetPasswordPage />} />
        <Route path="/login" element={<div>Página de login</div>} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('ResetPasswordPage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('shows invalid-link state when token is absent', () => {
    renderPage('')
    expect(screen.getByText(/link inválido/i)).toBeInTheDocument()
    expect(screen.queryByLabelText(/nova senha/i)).not.toBeInTheDocument()
  })

  it('renders password fields when token is present', () => {
    renderPage()
    expect(screen.getByLabelText(/nova senha/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/confirmar senha/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /redefinir senha/i })).toBeInTheDocument()
  })

  it('shows error when new password is empty', async () => {
    renderPage()
    fireEvent.submit(screen.getByRole('button', { name: /redefinir senha/i }).closest('form')!)
    await screen.findByText('Nova senha é obrigatória.')
    expect(authService.resetPassword).not.toHaveBeenCalled()
  })

  it('shows error when password is too short', async () => {
    renderPage()
    await userEvent.type(screen.getByLabelText(/nova senha/i), '123')
    await userEvent.type(screen.getByLabelText(/confirmar senha/i), '123')
    fireEvent.submit(screen.getByRole('button', { name: /redefinir senha/i }).closest('form')!)
    await screen.findByText(/mínimo 6 caracteres/i)
    expect(authService.resetPassword).not.toHaveBeenCalled()
  })

  it('shows error when passwords do not match', async () => {
    renderPage()
    await userEvent.type(screen.getByLabelText(/nova senha/i), 'senha123')
    await userEvent.type(screen.getByLabelText(/confirmar senha/i), 'senha999')
    fireEvent.submit(screen.getByRole('button', { name: /redefinir senha/i }).closest('form')!)
    await screen.findByText(/não conferem/i)
    expect(authService.resetPassword).not.toHaveBeenCalled()
  })

  it('calls resetPassword with token and redirects to /login on success', async () => {
    vi.mocked(authService.resetPassword).mockResolvedValueOnce(undefined)
    const toast = await import('react-hot-toast')
    renderPage('?token=valid-token-abc')

    await userEvent.type(screen.getByLabelText(/nova senha/i), 'nova-senha-123')
    await userEvent.type(screen.getByLabelText(/confirmar senha/i), 'nova-senha-123')
    fireEvent.submit(screen.getByRole('button', { name: /redefinir senha/i }).closest('form')!)

    await waitFor(() =>
      expect(authService.resetPassword).toHaveBeenCalledWith({
        token: 'valid-token-abc',
        newPassword: 'nova-senha-123',
      }),
    )
    expect(toast.default.success).toHaveBeenCalled()
    await screen.findByText('Página de login')
  })

  it('shows error toast on API failure', async () => {
    vi.mocked(authService.resetPassword).mockRejectedValueOnce(new Error('server error'))
    const toast = await import('react-hot-toast')
    renderPage()

    await userEvent.type(screen.getByLabelText(/nova senha/i), 'nova-senha-123')
    await userEvent.type(screen.getByLabelText(/confirmar senha/i), 'nova-senha-123')
    fireEvent.submit(screen.getByRole('button', { name: /redefinir senha/i }).closest('form')!)

    await waitFor(() => expect(toast.default.error).toHaveBeenCalled())
    expect(screen.queryByText('Página de login')).not.toBeInTheDocument()
  })
})
