import { forwardRef } from 'react'
import { Loader2 } from 'lucide-react'
import { cn } from '@/utils/cn'

type Variant = 'primary' | 'ghost' | 'danger'
type Size = 'sm' | 'md' | 'lg'

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  size?: Size
  isLoading?: boolean
  leftIcon?: React.ReactNode
}

const variantStyles: Record<Variant, string> = {
  primary: [
    'bg-brand-500 text-white',
    'hover:bg-brand-400 active:bg-brand-600',
    'shadow-glow hover:shadow-[0_0_28px_rgba(14,165,233,0.4)]',
    'disabled:bg-brand-900 disabled:text-brand-700 disabled:shadow-none',
  ].join(' '),
  ghost: [
    'bg-white/5 text-slate-300 border border-white/10',
    'hover:bg-white/10 hover:text-white active:bg-white/5',
    'disabled:text-slate-600 disabled:border-white/5',
  ].join(' '),
  danger: [
    'bg-red-600 text-white',
    'hover:bg-red-500 active:bg-red-700',
    'disabled:bg-red-900 disabled:text-red-700',
  ].join(' '),
}

const sizeStyles: Record<Size, string> = {
  sm: 'h-10 px-4 text-sm',
  md: 'h-12 px-6 text-base',
  lg: 'h-14 px-8 text-lg',
}

/**
 * Button — componente premium touch-friendly.
 * Altura mínima de 48px para áreas de toque acessíveis em mobile (WCAG 2.5.5).
 */
export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      variant = 'primary',
      size = 'md',
      isLoading = false,
      leftIcon,
      children,
      className,
      disabled,
      ...props
    },
    ref,
  ) => {
    const isDisabled = disabled || isLoading

    return (
      <button
        ref={ref}
        disabled={isDisabled}
        className={cn(
          // Base
          'relative inline-flex items-center justify-center gap-2',
          'w-full rounded-xl font-semibold',
          'transition-all duration-200 ease-out',
          'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:ring-offset-2 focus-visible:ring-offset-surface-900',
          'select-none cursor-pointer disabled:cursor-not-allowed',
          // Variante e tamanho
          variantStyles[variant],
          sizeStyles[size],
          className,
        )}
        {...props}
      >
        {isLoading ? (
          <Loader2 className="h-5 w-5 animate-spin" aria-hidden="true" />
        ) : (
          leftIcon
        )}
        <span>{children}</span>
      </button>
    )
  },
)

Button.displayName = 'Button'
