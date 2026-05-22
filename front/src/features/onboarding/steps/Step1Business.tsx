import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Building2 } from 'lucide-react'

import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'

const schema = z.object({
  tenantName: z.string().min(2, 'Nome deve ter ao menos 2 caracteres'),
  slug: z
    .string()
    .min(3, 'Link deve ter ao menos 3 caracteres')
    .max(50, 'Link deve ter no máximo 50 caracteres')
    .regex(/^[a-z0-9-]+$/, 'Apenas letras minúsculas, números e hífens'),
})

type Step1Data = z.infer<typeof schema>

function toSlug(name: string): string {
  return name
    .toLowerCase()
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/[^a-z0-9\s-]/g, '')
    .trim()
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .slice(0, 50)
}

interface Step1Props {
  defaultValues?: Partial<Step1Data>
  onNext: (data: Step1Data) => void
}

export function Step1Business({ defaultValues, onNext }: Step1Props) {
  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<Step1Data>({
    resolver: zodResolver(schema),
    defaultValues: { tenantName: '', slug: '', ...defaultValues },
  })

  const tenantName = watch('tenantName')

  useEffect(() => {
    if (!defaultValues?.slug) {
      setValue('slug', toSlug(tenantName), { shouldValidate: false })
    }
  }, [tenantName, defaultValues?.slug, setValue])

  return (
    <form onSubmit={handleSubmit(onNext)} className="flex flex-col gap-5" noValidate>
      <div>
        <h3 className="font-display text-lg font-bold text-white">Seu estabelecimento</h3>
        <p className="text-sm text-slate-500 mt-1">Como se chama o seu negócio?</p>
      </div>

      <Input
        id="tenantName"
        label="Nome do estabelecimento"
        placeholder="Ex: Barbearia do João"
        leftIcon={<Building2 className="h-4 w-4" />}
        error={errors.tenantName?.message}
        {...register('tenantName')}
      />

      <div>
        <Input
          id="slug"
          label="Link da agenda"
          placeholder="barbearia-do-joao"
          hint="Apenas letras minúsculas, números e hífens."
          error={errors.slug?.message}
          {...register('slug')}
        />
        <p className="text-xs text-slate-600 mt-1.5">
          agendainteligente.app/<span className="text-brand-400">{watch('slug') || '...'}</span>
        </p>
      </div>

      <Button type="submit" className="mt-2">
        Próximo
      </Button>
    </form>
  )
}
