import { useEffect, useRef } from 'react'
import { createPortal } from 'react-dom'
import { X } from 'lucide-react'
import { cn } from '@/shared/utils/cn'

interface ModalProps {
  isOpen: boolean
  onClose: () => void
  title: string
  description?: string
  children: React.ReactNode
  /** Largura máxima do modal. Default: 'md' */
  size?: 'sm' | 'md' | 'lg' | 'full'
  /** Impede fechar ao clicar no backdrop */
  disableBackdropClose?: boolean
}

const sizeStyles: Record<NonNullable<ModalProps['size']>, string> = {
  sm:   'max-w-sm',
  md:   'max-w-md',
  lg:   'max-w-lg',
  full: 'max-w-full mx-4',
}

const FOCUSABLE =
  'a[href],button:not([disabled]),input:not([disabled]),select:not([disabled]),textarea:not([disabled]),[tabindex]:not([tabindex="-1"])'

export function Modal({
  isOpen,
  onClose,
  title,
  description,
  children,
  size = 'md',
  disableBackdropClose = false,
}: ModalProps) {
  const overlayRef = useRef<HTMLDivElement>(null)
  const panelRef   = useRef<HTMLDivElement>(null)

  // Focus trap + Escape
  useEffect(() => {
    if (!isOpen) return

    const previousFocus = document.activeElement as HTMLElement

    // Foca o primeiro elemento focável dentro do modal
    const focusFirst = () => {
      const focusable = panelRef.current?.querySelectorAll<HTMLElement>(FOCUSABLE)
      focusable?.[0]?.focus()
    }
    // Pequeno delay para o portal estar no DOM
    const timer = setTimeout(focusFirst, 0)

    // Prende o Tab dentro do modal
    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        onClose()
        return
      }
      if (e.key !== 'Tab') return

      const focusable = Array.from(
        panelRef.current?.querySelectorAll<HTMLElement>(FOCUSABLE) ?? [],
      )
      if (focusable.length === 0) return

      const first = focusable[0]
      const last  = focusable[focusable.length - 1]

      if (e.shiftKey) {
        if (document.activeElement === first) {
          e.preventDefault()
          last.focus()
        }
      } else {
        if (document.activeElement === last) {
          e.preventDefault()
          first.focus()
        }
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    document.body.style.overflow = 'hidden'

    return () => {
      clearTimeout(timer)
      document.removeEventListener('keydown', handleKeyDown)
      document.body.style.overflow = ''
      previousFocus?.focus()
    }
  }, [isOpen, onClose])

  if (!isOpen) return null

  return createPortal(
    <div
      ref={overlayRef}
      className="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4"
      aria-modal="true"
      role="dialog"
      aria-labelledby="modal-title"
      aria-describedby={description ? 'modal-description' : undefined}
    >
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/60 backdrop-blur-sm animate-fade-in"
        onClick={disableBackdropClose ? undefined : onClose}
        aria-hidden="true"
      />

      {/* Painel */}
      <div
        ref={panelRef}
        className={cn(
          // Mobile: bottom sheet que cresce até 90% da tela
          'relative w-full rounded-t-3xl sm:rounded-2xl',
          'flex flex-col',
          'max-h-[90dvh] sm:max-h-[90vh]',
          // Desktop: largura limitada
          'sm:w-full',
          sizeStyles[size],
          'bg-surface-800 border border-white/10 shadow-glass',
          'animate-slide-up',
        )}
      >
        {/* Header — fixo, não rola */}
        <div className="flex items-start justify-between p-5 pb-3 flex-shrink-0">
          <div className="flex-1 pr-4">
            <h2
              id="modal-title"
              className="font-display text-lg font-bold text-white leading-snug"
            >
              {title}
            </h2>
            {description && (
              <p id="modal-description" className="text-sm text-slate-400 mt-1">
                {description}
              </p>
            )}
          </div>
          <button
            type="button"
            onClick={onClose}
            className="flex-shrink-0 flex items-center justify-center h-8 w-8 rounded-lg text-slate-400 hover:text-white hover:bg-white/10 transition-colors"
            aria-label="Fechar modal"
          >
            <X className="h-4 w-4" aria-hidden="true" />
          </button>
        </div>

        {/* Separador */}
        <div className="h-px bg-white/10 mx-5 flex-shrink-0" />

        {/* Conteúdo — rola quando necessário */}
        <div className="flex-1 overflow-y-auto overscroll-contain p-5">
          {children}
        </div>
      </div>
    </div>,
    document.body,
  )
}
