import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { User, Mail, Lock } from 'lucide-react'

import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { appToast } from '@/shared/lib/toast'
import { onboardingService } from '@/features/onboarding/services/onboarding.service'
import { authService } from '@/features/auth/services/auth.service'
import { useAuthStore } from '@/features/auth/store/authStore'

const schema = z
  .object({
    ownerName: z.string().min(2, 'Nome deve ter ao menos 2 caracteres'),
    ownerEmail: z.string().email('Informe um e-mail válido'),
    ownerPassword: z.string().min(8, 'Senha deve ter ao menos 8 caracteres'),
    confirmPassword: z.string(),
  })
  .refine((d) => d.ownerPassword === d.confirmPassword, {
    message: 'As senhas não coincidem',
    path: ['confirmPassword'],
  })

type Step2Data = z.infer<typeof schema>

interface Step2Props {
  tenantName: string
  slug: string
  onNext: () => void
  onBack: () => void
}

export function Step2Owner({ tenantName, slug, onNext, onBack }: Step2Props) {
  const setTokens = useAuthStore((s) => s.setTokens)
  const [isLoading, setIsLoading] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<Step2Data>({ resolver: zodResolver(schema) })

  async function onSubmit(data: Step2Data) {
    setIsLoading(true)
    const toastId = appToast.loading('Criando sua conta...')

    try {
      await onboardingService.onboard({
        tenantName,
        slug,
        ownerName: data.ownerName,
        ownerEmail: data.ownerEmail,
        ownerPassword: data.ownerPassword,
      })

      const auth = await authService.login({
        email: data.ownerEmail,
        password: data.ownerPassword,
      })

      setTokens(auth.token, auth.refreshToken)
      appToast.success('Conta criada com sucesso!', toastId)
      onNext()
    } catch (err: unknown) {
      const isSlugConflict =
        err !== null &&
        typeof err === 'object' &&
        'response' in err &&
        (err as { response?: { status?: number } }).response?.status === 409

      appToast.error(
        isSlugConflict
          ? 'Este link já está em uso. Volte e escolha outro nome.'
          : appToast.apiError(err, 'Erro ao criar conta. Tente novamente.'),
        toastId,
      )
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-5" noValidate>
      <div>
        <h3 className="font-display text-lg font-bold text-white">Dados do responsável</h3>
        <p className="text-sm text-slate-500 mt-1">
          Quem vai gerenciar a <span className="text-white">{tenantName}</span>?
        </p>
      </div>

      <Input
        id="ownerName"
        label="Seu nome"
        placeholder="João Silva"
        autoComplete="name"
        leftIcon={<User className="h-4 w-4" />}
        error={errors.ownerName?.message}
        {...register('ownerName')}
      />

      <Input
        id="ownerEmail"
        type="email"
        label="E-mail"
        placeholder="joao@barbearia.com"
        autoComplete="email"
        inputMode="email"
        leftIcon={<Mail className="h-4 w-4" />}
        error={errors.ownerEmail?.message}
        {...register('ownerEmail')}
      />

      <Input
        id="ownerPassword"
        type="password"
        label="Senha"
        placeholder="Mínimo 8 caracteres"
        autoComplete="new-password"
        leftIcon={<Lock className="h-4 w-4" />}
        error={errors.ownerPassword?.message}
        {...register('ownerPassword')}
      />

      <Input
        id="confirmPassword"
        type="password"
        label="Confirmar senha"
        placeholder="Repita a senha"
        autoComplete="new-password"
        leftIcon={<Lock className="h-4 w-4" />}
        error={errors.confirmPassword?.message}
        {...register('confirmPassword')}
      />

      <div className="flex gap-3 mt-2">
        <Button
          type="button"
          variant="ghost"
          onClick={onBack}
          disabled={isLoading}
          className="flex-1"
        >
          Voltar
        </Button>
        <Button type="submit" isLoading={isLoading} className="flex-1">
          Criar conta
        </Button>
      </div>
    </form>
  )
}
