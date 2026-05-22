import { render, screen } from '@testing-library/react'
import { Select } from '@/shared/components/ui/Select'

const options = [
  { value: 'a', label: 'Opção A' },
  { value: 'b', label: 'Opção B' },
  { value: 'c', label: 'Opção C', disabled: true },
]

describe('Select', () => {
  it('renders label when provided', () => {
    render(<Select id="sel" label="Categoria" options={options} />)
    expect(screen.getByLabelText('Categoria')).toBeInTheDocument()
  })

  it('renders all options', () => {
    render(<Select id="sel" options={options} />)
    expect(screen.getByRole('option', { name: 'Opção A' })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Opção B' })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Opção C' })).toBeInTheDocument()
  })

  it('renders placeholder as first disabled option', () => {
    render(<Select id="sel" options={options} placeholder="Selecione..." />)
    const placeholder = screen.getByRole('option', { name: 'Selecione...' })
    expect(placeholder).toBeDisabled()
  })

  it('disables individual options when option.disabled is true', () => {
    render(<Select id="sel" options={options} />)
    expect(screen.getByRole('option', { name: 'Opção C' })).toBeDisabled()
  })

  it('renders error message with role alert', () => {
    render(<Select id="sel" options={options} error="Seleção obrigatória." />)
    expect(screen.getByRole('alert')).toHaveTextContent('Seleção obrigatória.')
  })

  it('sets aria-invalid when error is provided', () => {
    render(<Select id="sel" options={options} error="Erro" />)
    expect(screen.getByRole('combobox')).toHaveAttribute('aria-invalid', 'true')
  })

  it('renders hint when no error', () => {
    render(<Select id="sel" options={options} hint="Escolha uma das opções." />)
    expect(screen.getByText('Escolha uma das opções.')).toBeInTheDocument()
  })

  it('hides hint when error is present', () => {
    render(<Select id="sel" options={options} error="Erro" hint="Dica" />)
    expect(screen.queryByText('Dica')).not.toBeInTheDocument()
  })
})
