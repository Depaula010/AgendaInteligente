import { CalendarDays } from 'lucide-react'

export function LoadingScreen() {
  return (
    <div
      className="min-h-screen w-full bg-surface-900 flex flex-col items-center justify-center gap-4"
      role="status"
      aria-label="Carregando aplicação"
    >
      <div className="flex items-center justify-center h-16 w-16 rounded-2xl bg-brand-500/20 border border-brand-500/30">
        <CalendarDays className="h-8 w-8 text-brand-400 animate-pulse-slow" aria-hidden="true" />
      </div>
      <div className="flex gap-1.5">
        <span className="h-2 w-2 rounded-full bg-brand-500 animate-bounce [animation-delay:-0.3s]" />
        <span className="h-2 w-2 rounded-full bg-brand-500 animate-bounce [animation-delay:-0.15s]" />
        <span className="h-2 w-2 rounded-full bg-brand-500 animate-bounce" />
      </div>
    </div>
  )
}