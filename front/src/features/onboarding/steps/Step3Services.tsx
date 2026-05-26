import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Trash2, Scissors } from 'lucide-react'

import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { CurrencyInput } from '@/shared/components/ui/CurrencyInput'
import { appToast } from '@/shared/lib/toast'
import { onboardingService } from '@/features/onboarding/services/onboarding.service'

const serviceSchema = z.object({
  name: z.string().min(2, 'Nome deve ter ao menos 2 caracteres'),
  durationMinutes: z
    .number({ error: 'Informe a duração em minutos' })
    .min(5, 'Mínimo 5 minutos')
    .max(480, 'Máximo 480 minutos'),
  price: z
    .number({ error: 'Informe o preço' })
    .min(0, 'Preço não pode ser negativo'),
})

type ServiceFormData = z.infer<typeof serviceSchema>

interface ServiceItem extends ServiceFormData {
  id: string
}

interface Step3Props {
  onNext: () => void
}

export function Step3Services({ onNext }: Step3Props) {
  const [services, setServices] = useState<ServiceItem[]>([])
  const [isAdding, setIsAdding] = useState(false)

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm<ServiceFormData>({
    resolver: zodResolver(serviceSchema),
    defaultValues: { name: '', durationMinutes: 30, price: 0 },
  })

  async function addService(data: ServiceFormData) {
    setIsAdding(true)
    try {
      await onboardingService.createService(data)
      setServices((prev) => [...prev, { ...data, id: crypto.randomUUID() }])
      reset({ name: '', durationMinutes: 30, price: 0 })
      appToast.success(`"${data.name}" adicionado`)
    } catch (err) {
      appToast.error(appToast.apiError(err, 'Erro ao salvar serviço.'))
    } finally {
      setIsAdding(false)
    }
  }

  function removeService(id: string) {
    setServices((prev) => prev.filter((s) => s.id !== id))
  }

  return (
    <div className="flex flex-col gap-5">
      <div>
        <h3 className="font-display text-lg font-bold text-white">Serviços oferecidos</h3>
        <p className="text-sm text-slate-500 mt-1">
          Adicione seus serviços agora ou pule para fazer depois.
        </p>
      </div>

      <form onSubmit={handleSubmit(addService)} className="flex flex-col gap-3" noValidate>
        <Input
          id="serviceName"
          label="Nome do serviço"
          placeholder="Ex: Corte de cabelo"
          leftIcon={<Scissors className="h-4 w-4" />}
          error={errors.name?.message}
          {...register('name')}
        />

        <div className="grid grid-cols-2 gap-3">
          <Input
            id="durationMinutes"
            type="number"
            label="Duração (min)"
            placeholder="30"
            min={5}
            max={480}
            error={errors.durationMinutes?.message}
            {...register('durationMinutes', { valueAsNumber: true })}
          />
          <CurrencyInput
            id="price"
            label="Preço (R$)"
            value={watch('price')}
            onChange={(v) => setValue('price', v, { shouldValidate: true })}
            error={errors.price?.message}
          />
        </div>

        <Button
          type="submit"
          variant="ghost"
          isLoading={isAdding}
          className="w-full"
        >
          <Plus className="h-4 w-4 mr-1.5" aria-hidden="true" />
          Adicionar serviço
        </Button>
      </form>

      {services.length > 0 && (
        <ul className="flex flex-col gap-2" aria-label="Serviços adicionados">
          {services.map((s) => (
            <li
              key={s.id}
              className="flex items-center justify-between rounded-xl border border-white/10 bg-white/5 px-4 py-3"
            >
              <div>
                <p className="text-sm font-medium text-white">{s.name}</p>
                <p className="text-xs text-slate-500">
                  {s.durationMinutes} min · {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(s.price)}
                </p>
              </div>
              <button
                type="button"
                onClick={() => removeService(s.id)}
                className="flex items-center justify-center h-8 w-8 rounded-lg text-slate-500 hover:text-red-400 hover:bg-red-400/10 transition-colors"
                aria-label={`Remover ${s.name}`}
              >
                <Trash2 className="h-4 w-4" aria-hidden="true" />
              </button>
            </li>
          ))}
        </ul>
      )}

      <div className="flex gap-3 pt-2 border-t border-white/10">
        <button
          type="button"
          onClick={onNext}
          className="flex-1 text-sm text-slate-500 hover:text-slate-300 transition-colors py-2"
        >
          {services.length === 0 ? 'Pular esta etapa' : 'Continuar'}
        </button>
        {services.length > 0 && (
          <Button type="button" onClick={onNext} className="flex-1">
            Continuar
          </Button>
        )}
      </div>
    </div>
  )
}
