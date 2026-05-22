import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Modal } from '@/shared/components/ui/Modal'
import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { appToast } from '@/shared/lib/toast'
import { servicosService } from '@/features/servicos/services/servicos.service'
import type { ServiceCatalogResponse } from '@/features/agenda/types/agenda.types'

// ── Color presets ──────────────────────────────────────────────────────────────

const PRESET_COLORS = [
  '#0ea5e9', '#22c55e', '#f59e0b', '#ef4444', '#8b5cf6',
  '#ec4899', '#f97316', '#14b8a6', '#6366f1', '#84cc16',
]

// ── Schema ─────────────────────────────────────────────────────────────────────

const schema = z.object({
  name:            z.string().min(1, 'Informe o nome'),
  durationMinutes: z.number({ error: 'Informe a duração' }).int().min(1, 'Mínimo 1 minuto'),
  price:           z.number({ error: 'Informe o preço' }).min(0, 'Preço não pode ser negativo'),
  description:     z.string().optional(),
  calendarColor:   z.string().optional(),
  isActive:        z.boolean(),
})

type FormValues = z.infer<typeof schema>

// ── Props ──────────────────────────────────────────────────────────────────────

interface ServiceFormModalProps {
  service?: ServiceCatalogResponse
  onClose: () => void
}

// ── Component ──────────────────────────────────────────────────────────────────

export function ServiceFormModal({ service, onClose }: ServiceFormModalProps) {
  const isEdit = !!service
  const queryClient = useQueryClient()

  const { register, handleSubmit, watch, setValue, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      name:            service?.name ?? '',
      durationMinutes: service?.durationMinutes ?? 30,
      price:           service?.price ?? 0,
      description:     service?.description ?? '',
      calendarColor:   service?.calendarColor ?? PRESET_COLORS[0],
      isActive:        service?.isActive ?? true,
    },
  })

  const selectedColor = watch('calendarColor') ?? PRESET_COLORS[0]
  const isActive      = watch('isActive')

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      isEdit
        ? servicosService.update(service.id, values)
        : servicosService.create(values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] })
      queryClient.invalidateQueries({ queryKey: ['services-all'] })
      appToast.success(isEdit ? 'Serviço atualizado.' : 'Serviço criado.')
      onClose()
    },
    onError: (err: unknown) => appToast.error(appToast.apiError(err, 'Erro ao salvar serviço.')),
  })

  return (
    <Modal
      isOpen
      onClose={onClose}
      title={isEdit ? 'Editar serviço' : 'Novo serviço'}
      size="md"
    >
      <form
        onSubmit={handleSubmit((v) => mutation.mutate(v))}
        className="flex flex-col gap-4"
        noValidate
      >
        {/* Nome */}
        <Input
          {...register('name')}
          id="name"
          label="Nome"
          placeholder="Ex: Corte de cabelo"
          error={errors.name?.message}
        />

        {/* Duração + Preço */}
        <div className="grid grid-cols-2 gap-3">
          <Input
            {...register('durationMinutes', { valueAsNumber: true })}
            id="durationMinutes"
            type="number"
            min={1}
            label="Duração (min)"
            placeholder="30"
            error={errors.durationMinutes?.message}
          />
          <Input
            {...register('price', { valueAsNumber: true })}
            id="price"
            type="number"
            min={0}
            step={0.01}
            label="Preço (R$)"
            placeholder="0,00"
            error={errors.price?.message}
          />
        </div>

        {/* Descrição */}
        <div className="flex flex-col gap-1.5">
          <label htmlFor="description" className="text-sm font-medium text-slate-300">
            Descrição <span className="text-slate-600 font-normal">(opcional)</span>
          </label>
          <textarea
            {...register('description')}
            id="description"
            rows={2}
            placeholder="Detalhes do serviço..."
            className="field-base resize-none"
          />
        </div>

        {/* Cor */}
        <div className="flex flex-col gap-2">
          <span className="text-sm font-medium text-slate-300">Cor no calendário</span>
          <div className="flex flex-wrap gap-2">
            {PRESET_COLORS.map((color) => (
              <button
                key={color}
                type="button"
                onClick={() => setValue('calendarColor', color)}
                className="h-7 w-7 rounded-full transition-transform hover:scale-110 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
                style={{
                  backgroundColor: color,
                  boxShadow:
                    selectedColor === color
                      ? `0 0 0 2px #0f172a, 0 0 0 4px ${color}`
                      : undefined,
                }}
                aria-label={`Cor ${color}`}
                aria-pressed={selectedColor === color}
              />
            ))}
          </div>
        </div>

        {/* Ativo (apenas no edit) */}
        {isEdit && (
          <div className="flex items-center justify-between py-1">
            <span className="text-sm font-medium text-slate-300">Status</span>
            <button
              type="button"
              onClick={() => setValue('isActive', !isActive)}
              className={`flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
                isActive
                  ? 'bg-emerald-500/15 text-emerald-400 border border-emerald-500/30'
                  : 'bg-surface-700 text-slate-400 border border-white/10'
              }`}
            >
              <span className={`h-2 w-2 rounded-full ${isActive ? 'bg-emerald-400' : 'bg-slate-500'}`} />
              {isActive ? 'Ativo' : 'Inativo'}
            </button>
          </div>
        )}

        {/* Actions */}
        <div className="flex gap-3 pt-1">
          <Button type="button" variant="ghost" onClick={onClose} className="flex-1">
            Cancelar
          </Button>
          <Button type="submit" isLoading={mutation.isPending} className="flex-1">
            {isEdit ? 'Salvar' : 'Criar serviço'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
