import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { ForgotPasswordPage } from '@/features/auth/pages/ForgotPassword'
import { authService } from '@/features/auth/services/auth.service'

vi.mock('@/features/auth/services/auth.service')
vi.mock('react-hot-toast', () => ({
  default: {
    loading: vi.fn(() => 'toast-id'),
    dismiss:  vi.fn(),
    error:    vi.fn(),
  },
}))

function renderPage() {
  return render(
    <MemoryRouter>
      <ForgotPasswordPage />
    </MemoryRouter>,
  )
}

describe('ForgotPasswordPage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('renders email input and submit button', () => {
    renderPage()
    expect(screen.getByLabelText(/e-mail/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /enviar link/i })).toBeInTheDocument()
  })

  it('shows validation error when email is empty', async () => {
    renderPage()
    fireEvent.submit(screen.getByRole('button', { name: /enviar link/i }).closest('form')!)
    await screen.findByText('E-mail é obrigatório.')
    expect(authService.forgotPassword).not.toHaveBeenCalled()
  })

  it('shows validation error when email is invalid', async () => {
    renderPage()
    await userEvent.type(screen.getByLabelText(/e-mail/i), 'nao-e-email')
    fireEvent.submit(screen.getByRole('button', { name: /enviar link/i }).closest('form')!)
    await screen.findByText('Informe um e-mail válido.')
    expect(authService.forgotPassword).not.toHaveBeenCalled()
  })

  it('calls forgotPassword and shows success state on valid submit', async () => {
    vi.mocked(authService.forgotPassword).mockResolvedValueOnce(undefined)
    renderPage()
    await userEvent.type(screen.getByLabelText(/e-mail/i), 'joao@barbearia.com')
    fireEvent.submit(screen.getByRole('button', { name: /enviar link/i }).closest('form')!)

    await waitFor(() =>
      expect(authService.forgotPassword).toHaveBeenCalledWith({ email: 'joao@barbearia.com' }),
    )
    await screen.findByText(/e-mail enviado/i)
    expect(screen.getByText('joao@barbearia.com')).toBeInTheDocument()
  })

  it('shows error toast when API call fails', async () => {
    const toast = await import('react-hot-toast')
    vi.mocked(authService.forgotPassword).mockRejectedValueOnce(new Error('network error'))
    renderPage()
    await userEvent.type(screen.getByLabelText(/e-mail/i), 'joao@barbearia.com')
    fireEvent.submit(screen.getByRole('button', { name: /enviar link/i }).closest('form')!)

    await waitFor(() => expect(toast.default.error).toHaveBeenCalled())
    expect(screen.queryByText(/e-mail enviado/i)).not.toBeInTheDocument()
  })

  it('link "Voltar para o login" is present', () => {
    renderPage()
    expect(screen.getByRole('link', { name: /voltar para o login/i })).toHaveAttribute(
      'href',
      '/login',
    )
  })
})
