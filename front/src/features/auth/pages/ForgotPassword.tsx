import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Mail, CheckCircle2, ArrowLeft } from 'lucide-react'
import axios from 'axios'
import toast from 'react-hot-toast'

import { AuthLayout } from '@/shared/components/layouts/AuthLayout'
import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { authService } from '@/features/auth/services/auth.service'
import { ROUTES } from '@/app/routes'

function validateEmail(email: string): string | undefined {
  if (!email) return 'E-mail é obrigatório.'
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) return 'Informe um e-mail válido.'
}

export function ForgotPasswordPage() {
  const [email, setEmail]       = useState('')
  const [emailError, setEmailError] = useState<string | undefined>()
  const [isLoading, setIsLoading]   = useState(false)
  const [sent, setSent]             = useState(false)

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()

    const error = validateEmail(email)
    if (error) {
      setEmailError(error)
      return
    }

    setIsLoading(true)
    const toastId = toast.loading('Enviando...')

    try {
      await authService.forgotPassword({ email })
      toast.dismiss(toastId)
      setSent(true)
    } catch (err) {
      const message =
        axios.isAxiosError(err) && err.response?.data?.message
          ? String(err.response.data.message)
          : 'Não foi possível enviar o e-mail. Tente novamente.'
      toast.error(message, { id: toastId })
    } finally {
      setIsLoading(false)
    }
  }

  if (sent) {
    return (
      <AuthLayout>
        <div className="flex flex-col items-center gap-4 text-center">
          <div className="flex items-center justify-center h-14 w-14 rounded-full bg-emerald-500/20 border border-emerald-500/30">
            <CheckCircle2 className="h-7 w-7 text-emerald-400" aria-hidden="true" />
          </div>
          <div>
            <h2 className="font-display text-xl font-bold text-white">E-mail enviado!</h2>
            <p className="text-sm text-slate-400 mt-2">
              Se <span className="text-white font-medium">{email}</span> estiver cadastrado,
              você receberá um link para redefinir sua senha em instantes.
            </p>
            <p className="text-xs text-slate-500 mt-3">Verifique também a caixa de spam.</p>
          </div>
          <Link
            to={ROUTES.LOGIN}
            className="mt-2 inline-flex items-center gap-2 text-sm text-brand-400 hover:text-brand-300 transition-colors"
          >
            <ArrowLeft className="h-4 w-4" aria-hidden="true" />
            Voltar para o login
          </Link>
        </div>
      </AuthLayout>
    )
  }

  return (
    <AuthLayout>
      <div className="flex flex-col gap-6">
        <div>
          <h2 className="font-display text-xl font-bold text-white">Esqueci minha senha</h2>
          <p className="text-sm text-slate-500 mt-1">
            Informe seu e-mail e enviaremos um link para redefinir sua senha.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
          <Input
            id="forgot-email"
            name="email"
            type="email"
            label="E-mail"
            placeholder="seuemail@exemplo.com"
            autoComplete="email"
            inputMode="email"
            leftIcon={<Mail className="h-4 w-4" />}
            value={email}
            onChange={(e) => {
              setEmail(e.target.value)
              if (emailError) setEmailError(undefined)
            }}
            error={emailError}
          />

          <Button type="submit" isLoading={isLoading} className="mt-2">
            Enviar link de redefinição
          </Button>
        </form>

        <Link
          to={ROUTES.LOGIN}
          className="inline-flex items-center justify-center gap-2 text-sm text-slate-500 hover:text-slate-300 transition-colors"
        >
          <ArrowLeft className="h-4 w-4" aria-hidden="true" />
          Voltar para o login
        </Link>
      </div>
    </AuthLayout>
  )
}
