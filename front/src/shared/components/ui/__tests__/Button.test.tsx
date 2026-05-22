import { render, screen, fireEvent } from '@testing-library/react'
import { Button } from '@/shared/components/ui/Button'

describe('Button', () => {
  it('renders children text', () => {
    render(<Button>Salvar</Button>)
    expect(screen.getByRole('button', { name: 'Salvar' })).toBeInTheDocument()
  })

  it('shows spinner and hides children text when isLoading', () => {
    render(<Button isLoading>Salvar</Button>)
    const btn = screen.getByRole('button')
    expect(btn.querySelector('svg')).toBeInTheDocument()
  })

  it('is disabled when isLoading is true', () => {
    render(<Button isLoading>Salvar</Button>)
    expect(screen.getByRole('button')).toBeDisabled()
  })

  it('is disabled when disabled prop is set', () => {
    render(<Button disabled>Salvar</Button>)
    expect(screen.getByRole('button')).toBeDisabled()
  })

  it('does not call onClick when disabled', () => {
    const onClick = vi.fn()
    render(<Button disabled onClick={onClick}>Salvar</Button>)
    fireEvent.click(screen.getByRole('button'))
    expect(onClick).not.toHaveBeenCalled()
  })

  it('calls onClick when enabled', () => {
    const onClick = vi.fn()
    render(<Button onClick={onClick}>Salvar</Button>)
    fireEvent.click(screen.getByRole('button'))
    expect(onClick).toHaveBeenCalledOnce()
  })

  it('renders leftIcon when not loading', () => {
    render(<Button leftIcon={<span data-testid="icon" />}>Salvar</Button>)
    expect(screen.getByTestId('icon')).toBeInTheDocument()
  })

  it('applies danger variant', () => {
    render(<Button variant="danger">Excluir</Button>)
    expect(screen.getByRole('button')).toHaveClass('bg-red-600')
  })

  it('applies ghost variant', () => {
    render(<Button variant="ghost">Cancelar</Button>)
    expect(screen.getByRole('button')).toHaveClass('bg-white/5')
  })
})
