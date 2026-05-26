import { useState } from 'react'
import { cn } from '@/shared/utils/cn'

interface CurrencyInputProps {
  id?: string
  label?: string
  error?: string
  value: number
  onChange: (value: number) => void
  disabled?: boolean
  className?: string
}

function toDisplay(reais: number): string {
  if (reais === 0) return ''
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(reais)
}

export function CurrencyInput({ id, label, error, value, onChange, disabled, className }: CurrencyInputProps) {
  const [display, setDisplay] = useState(() => toDisplay(value))

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const digits = e.target.value.replace(/\D/g, '')
    const cents = parseInt(digits || '0', 10)
    const reais = cents / 100
    setDisplay(digits ? toDisplay(reais) : '')
    onChange(reais)
  }

  return (
    <div className="flex flex-col gap-1.5">
      {label && (
        <label htmlFor={id} className="text-sm font-medium text-slate-300">
          {label}
        </label>
      )}

      <input
        id={id}
        inputMode="numeric"
        value={display}
        onChange={handleChange}
        disabled={disabled}
        placeholder="R$ 0,00"
        className={cn(
          'w-full rounded-xl border bg-white/5 backdrop-blur-sm',
          'text-base text-white placeholder:text-slate-600',
          'px-4 py-3.5',
          'transition-all duration-200',
          'focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent',
          !error && 'border-white/10 hover:border-white/20',
          error && 'border-red-500/60 focus:ring-red-500',
          className,
        )}
        aria-invalid={!!error}
        aria-describedby={error ? `${id}-error` : undefined}
      />

      {error && (
        <p id={`${id}-error`} className="text-sm text-red-400" role="alert">
          {error}
        </p>
      )}
    </div>
  )
}
