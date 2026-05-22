import { render, screen, fireEvent } from '@testing-library/react'
import { Modal } from '@/shared/components/ui/Modal'

function renderModal(props: Partial<Parameters<typeof Modal>[0]> = {}) {
  const onClose = vi.fn()
  render(
    <Modal isOpen={true} onClose={onClose} title="Título do modal" {...props}>
      <p>Conteúdo do modal</p>
    </Modal>,
  )
  return { onClose }
}

describe('Modal', () => {
  it('renders nothing when isOpen is false', () => {
    render(
      <Modal isOpen={false} onClose={vi.fn()} title="Modal">
        <p>Conteúdo</p>
      </Modal>,
    )
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('renders dialog when isOpen is true', () => {
    renderModal()
    expect(screen.getByRole('dialog')).toBeInTheDocument()
  })

  it('renders the title', () => {
    renderModal()
    expect(screen.getByRole('heading', { name: 'Título do modal' })).toBeInTheDocument()
  })

  it('renders children content', () => {
    renderModal()
    expect(screen.getByText('Conteúdo do modal')).toBeInTheDocument()
  })

  it('renders description when provided', () => {
    renderModal({ description: 'Descrição do modal' })
    expect(screen.getByText('Descrição do modal')).toBeInTheDocument()
  })

  it('calls onClose when close button is clicked', () => {
    const { onClose } = renderModal()
    fireEvent.click(screen.getByRole('button', { name: 'Fechar modal' }))
    expect(onClose).toHaveBeenCalledOnce()
  })

  it('calls onClose when Escape key is pressed', () => {
    const { onClose } = renderModal()
    fireEvent.keyDown(document, { key: 'Escape' })
    expect(onClose).toHaveBeenCalledOnce()
  })

  it('calls onClose when backdrop is clicked', () => {
    const { onClose } = renderModal()
    const dialog = screen.getByRole('dialog')
    const backdrop = dialog.querySelector('[aria-hidden="true"]') as HTMLElement
    fireEvent.click(backdrop)
    expect(onClose).toHaveBeenCalledOnce()
  })

  it('does not call onClose when panel content is clicked', () => {
    const { onClose } = renderModal()
    fireEvent.click(screen.getByText('Conteúdo do modal'))
    expect(onClose).not.toHaveBeenCalled()
  })

  it('does not call onClose on backdrop click when disableBackdropClose is true', () => {
    const { onClose } = renderModal({ disableBackdropClose: true })
    const dialog = screen.getByRole('dialog')
    const backdrop = dialog.querySelector('[aria-hidden="true"]') as HTMLElement
    fireEvent.click(backdrop)
    expect(onClose).not.toHaveBeenCalled()
  })

  it('sets overflow hidden on body when open', () => {
    renderModal()
    expect(document.body.style.overflow).toBe('hidden')
  })
})
