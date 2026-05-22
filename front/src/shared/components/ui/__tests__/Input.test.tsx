import { render, screen, fireEvent } from '@testing-library/react'
import { Input } from '@/shared/components/ui/Input'

describe('Input', () => {
  it('renders label when provided', () => {
    render(<Input id="email" label="E-mail" />)
    expect(screen.getByLabelText('E-mail')).toBeInTheDocument()
  })

  it('does not render label when omitted', () => {
    render(<Input id="email" />)
    expect(screen.queryByRole('label')).not.toBeInTheDocument()
  })

  it('renders error message with role alert', () => {
    render(<Input id="email" error="Campo obrigatório." />)
    expect(screen.getByRole('alert')).toHaveTextContent('Campo obrigatório.')
  })

  it('sets aria-invalid when error is provided', () => {
    render(<Input id="email" error="Campo obrigatório." />)
    expect(screen.getByRole('textbox')).toHaveAttribute('aria-invalid', 'true')
  })

  it('sets aria-invalid to false when no error', () => {
    render(<Input id="email" />)
    expect(screen.getByRole('textbox')).toHaveAttribute('aria-invalid', 'false')
  })

  it('renders hint text when no error', () => {
    render(<Input id="email" hint="Informe um e-mail válido." />)
    expect(screen.getByText('Informe um e-mail válido.')).toBeInTheDocument()
  })

  it('does not render hint when error is present', () => {
    render(<Input id="email" error="Erro" hint="Dica" />)
    expect(screen.queryByText('Dica')).not.toBeInTheDocument()
  })

  describe('password toggle', () => {
    it('starts with type password', () => {
      render(<Input id="pwd" type="password" />)
      expect(screen.getByLabelText('Mostrar senha')).toBeInTheDocument()
      // No accessible role for password input, query by type
      const input = document.getElementById('pwd') as HTMLInputElement
      expect(input.type).toBe('password')
    })

    it('toggles to text on button click', () => {
      render(<Input id="pwd" type="password" />)
      fireEvent.click(screen.getByLabelText('Mostrar senha'))
      const input = document.getElementById('pwd') as HTMLInputElement
      expect(input.type).toBe('text')
      expect(screen.getByLabelText('Ocultar senha')).toBeInTheDocument()
    })

    it('toggles back to password on second click', () => {
      render(<Input id="pwd" type="password" />)
      fireEvent.click(screen.getByLabelText('Mostrar senha'))
      fireEvent.click(screen.getByLabelText('Ocultar senha'))
      const input = document.getElementById('pwd') as HTMLInputElement
      expect(input.type).toBe('password')
    })
  })
})
