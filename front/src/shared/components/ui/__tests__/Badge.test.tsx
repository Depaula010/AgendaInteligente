import { render, screen } from '@testing-library/react'
import { Badge } from '@/shared/components/ui/Badge'

describe('Badge', () => {
  it('renders children text', () => {
    render(<Badge>Ativo</Badge>)
    expect(screen.getByText('Ativo')).toBeInTheDocument()
  })

  it('uses default variant by default', () => {
    render(<Badge>Default</Badge>)
    expect(screen.getByText('Default')).toHaveClass('bg-surface-700')
  })

  it.each([
    ['success', 'bg-emerald-500/15'],
    ['warning', 'bg-amber-500/15'],
    ['danger', 'bg-red-500/15'],
    ['info', 'bg-brand-500/15'],
  ] as const)('applies %s variant class', (variant, expectedClass) => {
    render(<Badge variant={variant}>{variant}</Badge>)
    expect(screen.getByText(variant)).toHaveClass(expectedClass)
  })

  it('applies outline variant', () => {
    render(<Badge variant="outline">Outline</Badge>)
    expect(screen.getByText('Outline')).toHaveClass('bg-transparent')
  })

  it('merges custom className', () => {
    render(<Badge className="font-bold">Custom</Badge>)
    expect(screen.getByText('Custom')).toHaveClass('font-bold')
  })
})
