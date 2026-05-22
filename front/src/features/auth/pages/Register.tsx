import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Mail, Lock, User, Briefcase } from 'lucide-react'
import toast from 'react-hot-toast'
import axios from 'axios'

import { AuthLayout } from '@/shared/components/layouts/AuthLayout'
import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { authService } from '@/features/auth/services/auth.service'
import { useAuthStore } from '@/features/auth/store/authStore'

interface FormState {
  businessName: string
  ownerName: string
  email: string
  password: string
  confirmPassword: string
}

interface FormErrors {
  businessName?: string
  ownerName?: string
  email?: string
  password?: string
  confirmPassword?: string
}

function validate(form: FormState): FormErrors {
  const errors: FormErrors = {}

  if (!form.businessName.trim()) errors.businessName = 'Nome do estabelecimento é obrigatório.'
  if (!form.ownerName.trim()) errors.ownerName = 'Seu nome é obrigatório.'

  if (!form.email) {
    errors.email = 'E-mail é obrigatório.'
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) {
    errors.email = 'Informe um e-mail válido.'
  }

  if (!form.password) {
    errors.password = 'Senha é obrigatória.'
  } else if (form.password.length < 8) {
    errors.password = 'A senha deve ter no mínimo 8 caracteres.'
  }

  if (!form.confirmPassword) {
    errors.confirmPassword = 'Confirme sua senha.'
  } else if (form.password !== form.confirmPassword) {
    errors.confirmPassword = 'As senhas não coincidem.'
  }

  return errors
}

export function RegisterPage() {
  const navigate = useNavigate()
  const setToken = useAuthStore((s) => s.setToken)

  const [form, setForm] = useState<FormState>({
    businessName: '',
    ownerName: '',
    email: '',
    password: '',
    confirmPassword: '',
  })
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
    const toastId = toast.loading('Criando sua conta...')

    try {
      const response = await authService.register({
        businessName: form.businessName,
        ownerName: form.ownerName,
        email: form.email,
        password: form.password,
        confirmPassword: form.confirmPassword,
      })

      setToken(response.token)
      toast.success('Conta criada com sucesso! Bem-vindo!', { id: toastId })
      navigate('/dashboard', { replace: true })
    } catch (err) {
      const message =
        axios.isAxiosError(err) && err.response?.data?.message
          ? String(err.response.data.message)
          : 'Erro ao criar sua conta. Tente novamente.'

      toast.error(message, { id: toastId })
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <AuthLayout>
      <div className="flex flex-col gap-5">
        <div>
          <h2 className="font-display text-xl font-bold text-white">Criar sua conta</h2>
          <p className="text-sm text-slate-500 mt-1">
            Configure sua agenda em menos de 2 minutos.
          </p>
        </div>

        <form
          id="register-form"
          onSubmit={handleSubmit}
          className="flex flex-col gap-4"
          noValidate
        >
          <Input
            id="register-business-name"
            name="businessName"
            type="text"
            label="Nome do estabelecimento"
            placeholder="Ex: Barbearia do João"
            autoComplete="organization"
            leftIcon={<Briefcase className="h-4 w-4" />}
            value={form.businessName}
            onChange={handleChange}
            error={errors.businessName}
          />

          <Input
            id="register-owner-name"
            name="ownerName"
            type="text"
            label="Seu nome"
            placeholder="Ex: João Silva"
            autoComplete="name"
            leftIcon={<User className="h-4 w-4" />}
            value={form.ownerName}
            onChange={handleChange}
            error={errors.ownerName}
          />

          <Input
            id="register-email"
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
            id="register-password"
            name="password"
            type="password"
            label="Senha"
            placeholder="Mínimo 8 caracteres"
            autoComplete="new-password"
            leftIcon={<Lock className="h-4 w-4" />}
            value={form.password}
            onChange={handleChange}
            error={errors.password}
            hint="Use letras, números e símbolos para uma senha segura."
          />

          <Input
            id="register-confirm-password"
            name="confirmPassword"
            type="password"
            label="Confirmar senha"
            placeholder="Repita a senha"
            autoComplete="new-password"
            leftIcon={<Lock className="h-4 w-4" />}
            value={form.confirmPassword}
            onChange={handleChange}
            error={errors.confirmPassword}
          />

          <Button
            type="submit"
            isLoading={isLoading}
            className="mt-2"
            aria-label="Criar conta gratuitamente"
          >
            Criar conta gratuitamente
          </Button>
        </form>

        <p className="text-center text-xs text-slate-600">
          Ao criar sua conta, você concorda com nossos{' '}
          <a href="#" className="text-brand-500 hover:text-brand-400 transition-colors">
            Termos de Uso
          </a>{' '}
          e{' '}
          <a href="#" className="text-brand-500 hover:text-brand-400 transition-colors">
            Política de Privacidade
          </a>
          .
        </p>

        <div className="relative flex items-center gap-3">
          <div className="flex-1 h-px bg-white/10" />
          <span className="text-xs text-slate-600">ou</span>
          <div className="flex-1 h-px bg-white/10" />
        </div>

        <p className="text-center text-sm text-slate-500">
          Já tem uma conta?{' '}
          <Link
            to="/login"
            className="text-brand-400 font-medium hover:text-brand-300 transition-colors"
          >
            Entrar agora
          </Link>
        </p>
      </div>
    </AuthLayout>
  )
}
