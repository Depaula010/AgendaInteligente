import { Fragment, useState } from 'react'
import { Link } from 'react-router-dom'
import { CalendarDays, Check } from 'lucide-react'
import { cn } from '@/shared/utils/cn'
import { ROUTES } from '@/app/routes'
import { Step1Business } from '@/features/onboarding/steps/Step1Business'
import { Step2Owner } from '@/features/onboarding/steps/Step2Owner'
import { Step3Services } from '@/features/onboarding/steps/Step3Services'
import { Step4Finish } from '@/features/onboarding/steps/Step4Finish'

const TOTAL_STEPS = 4

const STEP_LABELS = ['Estabelecimento', 'Responsável', 'Serviços', 'Concluído']

interface Step1Data {
  tenantName: string
  slug: string
}

function StepIndicator({ current }: { current: number }) {
  return (
    <div className="flex items-center justify-center mb-8" aria-label="Progresso do cadastro">
      {STEP_LABELS.map((label, i) => {
        const step = i + 1
        const isDone = step < current
        const isCurrent = step === current
        return (
          <Fragment key={step}>
            <div className="flex flex-col items-center gap-1">
              <div
                className={cn(
                  'flex items-center justify-center h-8 w-8 rounded-full text-xs font-semibold transition-all',
                  isDone && 'bg-brand-500 text-white',
                  isCurrent && 'border-2 border-brand-500 text-brand-400 bg-brand-500/10',
                  !isDone && !isCurrent && 'bg-surface-700 text-slate-500',
                )}
                aria-current={isCurrent ? 'step' : undefined}
              >
                {isDone ? <Check className="h-4 w-4" aria-hidden="true" /> : step}
              </div>
              <span
                className={cn(
                  'hidden sm:block text-xs',
                  isCurrent ? 'text-brand-400' : 'text-slate-600',
                )}
              >
                {label}
              </span>
            </div>
            {i < TOTAL_STEPS - 1 && (
              <div
                className={cn(
                  'h-px w-8 sm:w-12 mb-4 sm:mb-5 transition-colors',
                  step < current ? 'bg-brand-500' : 'bg-surface-700',
                )}
                aria-hidden="true"
              />
            )}
          </Fragment>
        )
      })}
    </div>
  )
}

export function OnboardingPage() {
  const [step, setStep] = useState(1)
  const [step1Data, setStep1Data] = useState<Step1Data>({ tenantName: '', slug: '' })

  function handleStep1Next(data: Step1Data) {
    setStep1Data(data)
    setStep(2)
  }

  return (
    <div className="min-h-screen w-full bg-auth-gradient flex flex-col items-center justify-center p-4 overflow-hidden">
      {/* Glow orbs (mesmos do AuthLayout) */}
      <div aria-hidden="true" className="pointer-events-none fixed inset-0 overflow-hidden">
        <div className="absolute -top-40 -right-40 h-96 w-96 rounded-full bg-brand-600/20 blur-3xl" />
        <div className="absolute -bottom-40 -left-40 h-96 w-96 rounded-full bg-brand-900/30 blur-3xl" />
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 h-64 w-64 rounded-full bg-brand-500/5 blur-2xl" />
      </div>

      <div className="relative z-10 w-full max-w-lg animate-slide-up">
        {/* Logo */}
        <div className="flex flex-col items-center mb-8">
          <div className="flex items-center justify-center h-14 w-14 rounded-2xl bg-brand-500/20 border border-brand-500/30 shadow-glow mb-3">
            <CalendarDays className="h-7 w-7 text-brand-400" aria-hidden="true" />
          </div>
          <h1 className="font-display text-2xl font-bold text-white tracking-tight">
            Agenda Inteligente
          </h1>
          <p className="text-sm text-slate-500 mt-1">Crie sua conta gratuita</p>
        </div>

        {/* Card */}
        <div className="rounded-2xl border border-white/10 bg-white/5 backdrop-blur-sm shadow-glass p-6 sm:p-8">
          <StepIndicator current={step} />

          {step === 1 && (
            <Step1Business
              defaultValues={step1Data.tenantName ? step1Data : undefined}
              onNext={handleStep1Next}
            />
          )}
          {step === 2 && (
            <Step2Owner
              tenantName={step1Data.tenantName}
              slug={step1Data.slug}
              onNext={() => setStep(3)}
              onBack={() => setStep(1)}
            />
          )}
          {step === 3 && <Step3Services onNext={() => setStep(4)} />}
          {step === 4 && <Step4Finish tenantName={step1Data.tenantName} />}
        </div>

        <p className="text-center text-xs text-slate-600 mt-6">
          Já tem uma conta?{' '}
          <Link to={ROUTES.LOGIN} className="text-brand-400 hover:text-brand-300 transition-colors">
            Entrar
          </Link>
        </p>
      </div>
    </div>
  )
}
