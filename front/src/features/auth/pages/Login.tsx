import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Mail, Lock } from 'lucide-react'
import toast from 'react-hot-toast'
import axios from 'axios'

import { AuthLayout } from '@/shared/components/layouts/AuthLayout'
import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { authService } from '@/features/auth/services/auth.service'
import { useAuthStore } from '@/features/auth/store/authStore'

interface FormState {
  email: string
  password: string
}

interface FormErrors {
  email?: string
  password?: string
}

function validate(form: FormState): FormErrors {
  const errors: FormErrors = {}

  if (!form.email) {
    errors.email = 'E-mail é obrigatório.'
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) {
    errors.email = 'Informe um e-mail válido.'
  }

  if (!form.password) {
    errors.password = 'Senha é obrigatória.'
  } else if (form.password.length < 6) {
    errors.password = 'A senha deve ter no mínimo 6 caracteres.'
  }

  return errors
}

export function LoginPage() {
  const navigate = useNavigate()
  const setToken = useAuthStore((s) => s.setToken)

  const [form, setForm] = useState<FormState>({ email: '', password: '' })
  const [errors, setErrors] = useState<FormErrors>({})
  const [isLoading, setIsLoading] = useState(false)

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
    const toastId = toast.loading('Entrando...')

    try {
      const response = await authService.login({
        email: form.email,
        password: form.password,
      })

      setToken(response.token)
      toast.success('Bem-vindo de volta!', { id: toastId })
      navigate('/dashboard', { replace: true })
    } catch (err) {
      const message =
        axios.isAxiosError(err) && err.response?.data?.message
          ? String(err.response.data.message)
          : 'E-mail ou senha incorretos. Tente novamente.'

      toast.error(message, { id: toastId })
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <AuthLayout>
      <div className="flex flex-col gap-6">
        <div>
          <h2 className="font-display text-xl font-bold text-white">Entrar na sua conta</h2>
          <p className="text-sm text-slate-500 mt-1">
            Acesse o painel de controle da sua agenda.
          </p>
        </div>

        <form id="login-form" onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
          <Input
            id="login-email"
            name="email"
            type="email"
            label="E-mail"
            placeholder="seuemail@exemplo.com"
            autoComplete="email"
            inputMode="email"
            leftIcon={<Mail className="h-4 w-4" />}
            value={form.email}
            onChange={handleChange}
            error={errors.email}
          />

          <Input
            id="login-password"
            name="password"
            type="password"
            label="Senha"
            placeholder="••••••••"
            autoComplete="current-password"
            leftIcon={<Lock className="h-4 w-4" />}
            value={form.password}
            onChange={handleChange}
            error={errors.password}
          />

          <div className="flex justify-end -mt-2">
            <Link
              to="/esqueci-senha"
              className="text-xs text-brand-400 hover:text-brand-300 transition-colors"
            >
              Esqueci minha senha
            </Link>
          </div>

          <Button type="submit" isLoading={isLoading} className="mt-2" aria-label="Entrar na conta">
            Entrar
          </Button>
        </form>

        <div className="relative flex items-center gap-3">
          <div className="flex-1 h-px bg-white/10" />
          <span className="text-xs text-slate-600">ou</span>
          <div className="flex-1 h-px bg-white/10" />
        </div>

        <p className="text-center text-sm text-slate-500">
          Ainda não tem uma conta?{' '}
          <Link
            to="/cadastro"
            className="text-brand-400 font-medium hover:text-brand-300 transition-colors"
          >
            Cadastre-se gratuitamente
          </Link>
        </p>
      </div>
    </AuthLayout>
  )
}
