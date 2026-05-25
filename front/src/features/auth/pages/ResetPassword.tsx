import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { Lock, ArrowLeft, AlertTriangle } from 'lucide-react'
import axios from 'axios'
import toast from 'react-hot-toast'

import { AuthLayout } from '@/shared/components/layouts/AuthLayout'
import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { authService } from '@/features/auth/services/auth.service'
import { ROUTES } from '@/app/routes'

interface FormState {
  newPassword: string
  confirmPassword: string
}

interface FormErrors {
  newPassword?: string
  confirmPassword?: string
}

function validate(form: FormState): FormErrors {
  const errors: FormErrors = {}

  if (!form.newPassword) {
    errors.newPassword = 'Nova senha é obrigatória.'
  } else if (form.newPassword.length < 6) {
    errors.newPassword = 'A senha deve ter no mínimo 6 caracteres.'
  }

  if (!form.confirmPassword) {
    errors.confirmPassword = 'Confirmação de senha é obrigatória.'
  } else if (form.newPassword !== form.confirmPassword) {
    errors.confirmPassword = 'As senhas não conferem.'
  }

  return errors
}

export function ResetPasswordPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token')

  const [form, setForm]     = useState<FormState>({ newPassword: '', confirmPassword: '' })
  const [errors, setErrors] = useState<FormErrors>({})
  const [isLoading, setIsLoading] = useState(false)

  if (!token) {
    return (
      <AuthLayout>
        <div className="flex flex-col items-center gap-4 text-center">
          <div className="flex items-center justify-center h-14 w-14 rounded-full bg-red-500/20 border border-red-500/30">
            <AlertTriangle className="h-7 w-7 text-red-400" aria-hidden="true" />
          </div>
          <div>
            <h2 className="font-display text-xl font-bold text-white">Link inválido</h2>
            <p className="text-sm text-slate-400 mt-2">
              Este link de redefinição de senha é inválido ou expirou.
            </p>
          </div>
          <Link
            to={ROUTES.FORGOT_PASSWORD}
            className="mt-2 inline-flex items-center gap-2 text-sm text-brand-400 hover:text-brand-300 transition-colors"
          >
            <ArrowLeft className="h-4 w-4" aria-hidden="true" />
            Solicitar novo link
          </Link>
        </div>
      </AuthLayout>
    )
  }

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const { name, value } = e.target
    setForm((prev) => ({ ...prev, [name]: value }))
    if (errors[name as keyof FormErrors]) {
      setErrors((prev) => ({ ...prev, [name]: undefined }))
    }
  }

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()

    const validationErrors = validate(form)
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors)
      return
    }

    setIsLoading(true)
    const toastId = toast.loading('Redefinindo senha...')

    try {
      await authService.resetPassword({ token: token!, newPassword: form.newPassword })
      toast.success('Senha redefinida com sucesso!', { id: toastId })
      navigate(ROUTES.LOGIN, { replace: true })
    } catch (err) {
      const isExpired =
        axios.isAxiosError(err) && err.response?.status === 400

      const message = isExpired
        ? 'Link expirado ou inválido. Solicite um novo.'
        : 'Não foi possível redefinir a senha. Tente novamente.'

      toast.error(message, { id: toastId })
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <AuthLayout>
      <div className="flex flex-col gap-6">
        <div>
          <h2 className="font-display text-xl font-bold text-white">Redefinir senha</h2>
          <p className="text-sm text-slate-500 mt-1">
            Crie uma nova senha para acessar sua conta.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
          <Input
            id="new-password"
            name="newPassword"
            type="password"
            label="Nova senha"
            placeholder="••••••••"
            autoComplete="new-password"
            leftIcon={<Lock className="h-4 w-4" />}
            value={form.newPassword}
            onChange={handleChange}
            error={errors.newPassword}
          />

          <Input
            id="confirm-password"
            name="confirmPassword"
            type="password"
            label="Confirmar senha"
            placeholder="••••••••"
            autoComplete="new-password"
            leftIcon={<Lock className="h-4 w-4" />}
            value={form.confirmPassword}
            onChange={handleChange}
            error={errors.confirmPassword}
          />

          <Button type="submit" isLoading={isLoading} className="mt-2">
            Redefinir senha
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
