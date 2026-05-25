import { useEffect, useState } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { AlertCircle, CheckCircle2, Search, UserPlus, X } from 'lucide-react'
import type { AxiosError } from 'axios'
import { Modal } from '@/shared/components/ui/Modal'
import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { Select } from '@/shared/components/ui/Select'
import { appToast } from '@/shared/lib/toast'
import { agendaService } from '@/features/agenda/services/agenda.service'
import type { ConflictInfo, CustomerResponse, RecurringConflictInfo } from '@/features/agenda/types/agenda.types'

// ── Schema ─────────────────────────────────────────────────────────────────────

const schema = z
  .object({
    professionalId: z.string().min(1, 'Selecione um profissional'),
    serviceId: z.string().min(1, 'Selecione um serviço'),
    date: z.string().min(1, 'Selecione a data'),
    startDateTime: z.string().min(1, 'Selecione um horário'),
    phone: z.string().min(8, 'Informe o telefone'),
    customerName: z.string().optional(),
    notes: z.string().optional(),
    recurring: z.boolean(),
    repeatWeeklyCount: z.number().min(1).max(52),
  })
  .refine(
    () => true,
    { message: '', path: [] },
  )

type FormValues = z.infer<typeof schema>

// ── Props ──────────────────────────────────────────────────────────────────────

interface CreateScheduleModalProps {
  isOpen: boolean
  onClose: () => void
  initialStart?: Date
}

// ── Component ──────────────────────────────────────────────────────────────────

export function CreateScheduleModal({ isOpen, onClose, initialStart }: CreateScheduleModalProps) {
  const queryClient = useQueryClient()
  const [foundCustomer, setFoundCustomer] = useState<CustomerResponse | null | undefined>(undefined)
  const [searchingPhone, setSearchingPhone] = useState(false)
  const [conflictInfo, setConflictInfo] = useState<ConflictInfo | null>(null)
  const [recurringConflict, setRecurringConflict] = useState<RecurringConflictInfo | null>(null)
  const [selectedSlot, setSelectedSlot] = useState<string | null>(null)

  const defaultDate = initialStart
    ? initialStart.toISOString().split('T')[0]
    : new Date().toISOString().split('T')[0]

  const {
    register,
    control,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      date: defaultDate,
      professionalId: '',
      serviceId: '',
      startDateTime: '',
      phone: '',
      customerName: '',
      notes: '',
      recurring: false,
      repeatWeeklyCount: 4,
    },
  })

  const watchedProfessional = watch('professionalId')
  const watchedService = watch('serviceId')
  const watchedDate = watch('date')
  const watchedRecurring = watch('recurring')

  // Reset slot when inputs change
  useEffect(() => {
    setSelectedSlot(null)
    setValue('startDateTime', '')
  }, [watchedProfessional, watchedService, watchedDate, setValue])

  const { data: professionals = [] } = useQuery({
    queryKey: ['professionals'],
    queryFn: agendaService.getProfessionals,
    staleTime: 5 * 60 * 1000,
  })

  const { data: services = [] } = useQuery({
    queryKey: ['services'],
    queryFn: agendaService.getServices,
    staleTime: 5 * 60 * 1000,
  })

  const { data: slotsData, isFetching: loadingSlots } = useQuery({
    queryKey: ['slots', watchedProfessional, watchedService, watchedDate],
    queryFn: () =>
      agendaService.getAvailableSlots(watchedProfessional, watchedService, watchedDate),
    enabled: !!watchedProfessional && !!watchedService && !!watchedDate,
    staleTime: 0,
  })

  async function handlePhoneSearch() {
    const phone = watch('phone').trim()
    if (!phone) return
    setSearchingPhone(true)
    try {
      const customer = await agendaService.getCustomerByPhone(phone)
      setFoundCustomer(customer)
    } finally {
      setSearchingPhone(false)
    }
  }

  const createMutation = useMutation({
    mutationFn: async (values: FormValues) => {
      let customerId: string

      if (foundCustomer) {
        customerId = foundCustomer.id
      } else {
        if (!values.customerName?.trim()) {
          throw new Error('MISSING_NAME')
        }
        const created = await agendaService.createCustomer({
          name: values.customerName.trim(),
          phoneNumber: values.phone.trim(),
        })
        customerId = created.id
      }

      if (values.recurring) {
        return agendaService.createRecurringSchedule({
          customerId,
          professionalId: values.professionalId,
          serviceId: values.serviceId,
          startDateTime: values.startDateTime,
          repeatWeeklyCount: values.repeatWeeklyCount,
          notes: values.notes || undefined,
        })
      }

      return agendaService.createSchedule({
        customerId,
        professionalId: values.professionalId,
        serviceId: values.serviceId,
        startDateTime: values.startDateTime,
        notes: values.notes || undefined,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schedules'] })
      appToast.success(
        watch('recurring')
          ? `${watch('repeatWeeklyCount')} agendamentos recorrentes criados!`
          : 'Agendamento criado com sucesso!',
      )
      onClose()
    },
    onError: (err: unknown) => {
      if ((err as Error).message === 'MISSING_NAME') {
        appToast.error('Informe o nome do cliente.')
        return
      }
      const axiosErr = err as AxiosError<ConflictInfo & RecurringConflictInfo>
      if (axiosErr.response?.status === 409) {
        if (axiosErr.response.data?.conflictingDates) {
          setRecurringConflict(axiosErr.response.data as RecurringConflictInfo)
          return
        }
        if (axiosErr.response.data?.suggestedAlternatives) {
          setConflictInfo(axiosErr.response.data as ConflictInfo)
          return
        }
      }
      appToast.error(appToast.apiError(err, 'Erro ao criar agendamento.'))
    },
  })

  const professionalOptions = professionals.map((p) => ({ value: p.id, label: p.name }))
  const serviceOptions = services
    .filter((s) => s.isActive)
    .map((s) => ({ value: s.id, label: `${s.name} (${s.durationMinutes} min)` }))

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Novo agendamento" size="lg">
      <form
        onSubmit={handleSubmit((values) => {
          setConflictInfo(null)
          setRecurringConflict(null)
          createMutation.mutate(values as FormValues)
        })}
        className="flex flex-col gap-5"
        noValidate
      >
        {/* Professional */}
        <Controller
          name="professionalId"
          control={control}
          render={({ field }) => (
            <Select
              {...field}
              id="professionalId"
              label="Profissional"
              placeholder="Selecione..."
              options={professionalOptions}
              error={errors.professionalId?.message}
            />
          )}
        />

        {/* Service */}
        <Controller
          name="serviceId"
          control={control}
          render={({ field }) => (
            <Select
              {...field}
              id="serviceId"
              label="Serviço"
              placeholder="Selecione..."
              options={serviceOptions}
              error={errors.serviceId?.message}
            />
          )}
        />

        {/* Date */}
        <Input
          {...register('date')}
          id="date"
          type="date"
          label="Data"
          min={new Date().toISOString().split('T')[0]}
          error={errors.date?.message}
        />

        {/* Available slots */}
        {watchedProfessional && watchedService && watchedDate && (
          <div className="flex flex-col gap-2">
            <p className="text-sm font-medium text-slate-300">Horário disponível</p>

            {loadingSlots && (
              <p className="text-xs text-slate-500 text-center py-3">Carregando horários...</p>
            )}

            {!loadingSlots && slotsData && slotsData.slots.length === 0 && (
              <p className="text-xs text-slate-500 text-center py-3 border border-white/10 rounded-xl">
                Nenhum horário disponível nesta data.
              </p>
            )}

            {!loadingSlots && slotsData && slotsData.slots.length > 0 && (
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
                {slotsData.slots.map((slot) => {
                  const label = new Date(slot).toLocaleTimeString('pt-BR', {
                    hour: '2-digit',
                    minute: '2-digit',
                  })
                  const isSelected = slot === selectedSlot
                  return (
                    <button
                      key={slot}
                      type="button"
                      onClick={() => {
                        setSelectedSlot(slot)
                        setValue('startDateTime', slot, { shouldValidate: true })
                        setConflictInfo(null)
                      }}
                      className={`rounded-lg py-2.5 text-sm font-medium transition-colors ${
                        isSelected
                          ? 'bg-brand-500 text-white shadow-md'
                          : 'bg-white/5 text-slate-300 hover:bg-white/10 border border-white/10'
                      }`}
                    >
                      {label}
                    </button>
                  )
                })}
              </div>
            )}

            {errors.startDateTime && !selectedSlot && (
              <p className="text-sm text-red-400">{errors.startDateTime.message}</p>
            )}
          </div>
        )}

        {/* Conflict banner */}
        {conflictInfo && (
          <div className="rounded-xl border border-orange-500/30 bg-orange-500/10 p-4 flex flex-col gap-3">
            <div className="flex items-start gap-2">
              <AlertCircle className="h-4 w-4 text-orange-400 flex-shrink-0 mt-0.5" />
              <p className="text-sm text-orange-300">{conflictInfo.error}</p>
            </div>
            {conflictInfo.suggestedAlternatives.length > 0 && (
              <div className="flex flex-col gap-1.5">
                <p className="text-xs text-slate-400 font-medium">Horários alternativos:</p>
                <div className="flex flex-wrap gap-2">
                  {conflictInfo.suggestedAlternatives.map((alt) => {
                    const label = new Date(alt).toLocaleTimeString('pt-BR', {
                      hour: '2-digit',
                      minute: '2-digit',
                    })
                    return (
                      <button
                        key={alt}
                        type="button"
                        onClick={() => {
                          setSelectedSlot(alt)
                          setValue('startDateTime', alt, { shouldValidate: true })
                          setConflictInfo(null)
                        }}
                        className="rounded-lg px-3 py-1.5 text-sm font-medium bg-orange-500/20 text-orange-300 hover:bg-orange-500/30 border border-orange-500/30 transition-colors"
                      >
                        {label}
                      </button>
                    )
                  })}
                </div>
              </div>
            )}
          </div>
        )}

        {/* Customer search */}
        <div className="flex flex-col gap-3">
          <p className="text-sm font-medium text-slate-300">Cliente</p>

          <div className="flex gap-2">
            <Input
              {...register('phone')}
              id="phone"
              type="tel"
              placeholder="+55 11 99999-9999"
              error={errors.phone?.message}
              className="flex-1"
            />
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={handlePhoneSearch}
              isLoading={searchingPhone}
              leftIcon={<Search className="h-4 w-4" aria-hidden="true" />}
              className="self-start mt-0 whitespace-nowrap"
            >
              Buscar
            </Button>
          </div>

          {/* Found customer */}
          {foundCustomer && (
            <div className="flex items-center justify-between rounded-xl bg-green-500/10 border border-green-500/20 px-4 py-3">
              <div className="flex items-center gap-3">
                <CheckCircle2 className="h-4 w-4 text-green-400 flex-shrink-0" aria-hidden="true" />
                <div>
                  <p className="text-sm font-medium text-white">{foundCustomer.name}</p>
                  <p className="text-xs text-slate-400">{foundCustomer.phoneNumber}</p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setFoundCustomer(undefined)}
                className="text-slate-500 hover:text-white transition-colors"
                aria-label="Remover cliente selecionado"
              >
                <X className="h-4 w-4" aria-hidden="true" />
              </button>
            </div>
          )}

          {/* Not found — inline form */}
          {foundCustomer === null && (
            <div className="flex flex-col gap-3 rounded-xl border border-white/10 bg-white/3 p-4">
              <div className="flex items-center gap-2">
                <UserPlus className="h-4 w-4 text-slate-400" aria-hidden="true" />
                <p className="text-sm text-slate-400">Cliente não encontrado. Informe o nome:</p>
              </div>
              <Input
                {...register('customerName')}
                id="customerName"
                placeholder="Nome completo"
                error={errors.customerName?.message}
              />
            </div>
          )}
        </div>

        {/* Notes */}
        <Input
          {...register('notes')}
          id="notes"
          label="Observações (opcional)"
          placeholder="Informações adicionais..."
          error={errors.notes?.message}
        />

        {/* Recurring */}
        <div className="flex flex-col gap-3">
          <label className="flex items-center gap-3 cursor-pointer select-none">
            <input
              type="checkbox"
              {...register('recurring')}
              className="h-4 w-4 rounded border-white/20 bg-white/5 accent-brand-500"
            />
            <span className="text-sm text-slate-300">Repetir semanalmente</span>
          </label>

          {watchedRecurring && (
            <Input
              {...register('repeatWeeklyCount', { valueAsNumber: true })}
              id="repeatWeeklyCount"
              type="number"
              label="Número de semanas (máx. 52)"
              min={1}
              max={52}
              error={errors.repeatWeeklyCount?.message}
            />
          )}
        </div>

        {/* Recurring conflict banner */}
        {recurringConflict && (
          <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-4 flex flex-col gap-2">
            <div className="flex items-start gap-2">
              <AlertCircle className="h-4 w-4 text-red-400 flex-shrink-0 mt-0.5" />
              <p className="text-sm text-red-300">{recurringConflict.error}</p>
            </div>
            {recurringConflict.conflictingDates.length > 0 && (
              <div className="flex flex-col gap-1">
                <p className="text-xs text-slate-400 font-medium">Datas com conflito:</p>
                <ul className="list-disc list-inside text-xs text-slate-400">
                  {recurringConflict.conflictingDates.map((d) => (
                    <li key={d}>
                      {new Date(d).toLocaleDateString('pt-BR', {
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit',
                      })}
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        )}

        {/* Submit */}
        <div className="flex gap-3 pt-1">
          <Button type="button" variant="ghost" onClick={onClose} className="flex-1">
            Cancelar
          </Button>
          <Button type="submit" isLoading={createMutation.isPending} className="flex-1">
            {watchedRecurring ? 'Criar série recorrente' : 'Criar agendamento'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
