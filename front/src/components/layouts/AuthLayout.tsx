import type { ReactNode } from 'react'
import { CalendarDays } from 'lucide-react'

interface AuthLayoutProps {
  children: ReactNode
}

/**
 * AuthLayout — wrapper das telas de login/cadastro.
 * Design: fundo escuro gradiente + card glassmorphism centralizado.
 * Mobile First: ocupa 100% da viewport no mobile, card fixo no desktop.
 */
export function AuthLayout({ children }: AuthLayoutProps) {
  return (
    <div className="min-h-screen w-full bg-auth-gradient flex flex-col items-center justify-center p-4 overflow-hidden">
      {/* Orbs decorativos de fundo — apenas visual, aria-hidden */}
      <div aria-hidden="true" className="pointer-events-none fixed inset-0 overflow-hidden">
        <div className="absolute -top-40 -right-40 h-96 w-96 rounded-full bg-brand-600/20 blur-3xl" />
        <div className="absolute -bottom-40 -left-40 h-96 w-96 rounded-full bg-brand-900/30 blur-3xl" />
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 h-64 w-64 rounded-full bg-brand-500/5 blur-2xl" />
      </div>

      {/* Card principal */}
      <div className="relative z-10 w-full max-w-md animate-slide-up">
        {/* Logo / Branding */}
        <div className="flex flex-col items-center mb-8">
          <div className="flex items-center justify-center h-16 w-16 rounded-2xl bg-brand-500/20 border border-brand-500/30 shadow-glow mb-4">
            <CalendarDays className="h-8 w-8 text-brand-400" aria-hidden="true" />
          </div>
          <h1 className="font-display text-2xl font-bold text-white tracking-tight">
            Agenda Inteligente
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            Painel do Profissional
          </p>
        </div>

        {/* Conteúdo da tela (form) */}
        <div className="rounded-2xl border border-white/10 bg-white/5 backdrop-blur-sm shadow-glass p-6 sm:p-8">
          {children}
        </div>

        {/* Rodapé */}
        <p className="text-center text-xs text-slate-600 mt-6">
          © {new Date().getFullYear()} Agenda Inteligente · Todos os direitos reservados
        </p>
      </div>
    </div>
  )
}
