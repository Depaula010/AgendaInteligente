import { useEffect, useRef, useState } from 'react'
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
import { fmtDateTime, fmtTime } from '@/shared/utils/date'

// ── Schema ─────────────────────────────────────────────────────────────────────

const schema = z.object({
  professionalId: z.string().min(1, 'Selecione um profissional'),
  serviceId: z.string().min(1, 'Selecione um serviço'),
  date: z.string().min(1, 'Selecione a data'),
  startDateTime: z.string().min(1, 'Selecione um horário'),
  newCustomerName: z.string().optional(),
  newCustomerPhone: z.string().optional(),
  notes: z.string().optional(),
  repeatType: z.enum(['none', 'weekly', 'monthly']),
  repeatCount: z.number().min(1).max(260).optional(),
  indefinite: z.boolean(),
})

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

  // customer state
  const [foundCustomer, setFoundCustomer] = useState<CustomerResponse | null | undefined>(undefined)
  const [searchQuery, setSearchQuery] = useState('')
  const [debouncedQuery, setDebouncedQuery] = useState('')
  const [showDropdown, setShowDropdown] = useState(false)
  const [creatingNew, setCreatingNew] = useState(false)
  const searchRef = useRef<HTMLDivElement>(null)

  // schedule state
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
      newCustomerName: '',
      newCustomerPhone: '',
      notes: '',
      repeatType: 'none' as const,
      repeatCount: 4,
      indefinite: false,
    },
  })

  const watchedProfessional = watch('professionalId')
  const watchedService = watch('serviceId')
  const watchedDate = watch('date')
  const watchedRepeatType = watch('repeatType')
  const watchedIndefinite = watch('indefinite')

  // Reset slot when inputs change
  useEffect(() => {
    setSelectedSlot(null)
    setValue('startDateTime', '')
  }, [watchedProfessional, watchedService, watchedDate, setValue])

  // Debounce search query
  useEffect(() => {
    const t = setTimeout(() => setDebouncedQuery(searchQuery), 300)
    return () => clearTimeout(t)
  }, [searchQuery])

  // Close dropdown on outside click
  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (searchRef.current && !searchRef.current.contains(e.target as Node)) {
        setShowDropdown(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

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

  const { data: searchResults = [], isFetching: searchFetching } = useQuery({
    queryKey: ['customers-search', debouncedQuery],
    queryFn: () => agendaService.searchCustomers(debouncedQuery),
    enabled: debouncedQuery.length >= 2 && !foundCustomer,
    staleTime: 30_000,
  })

  function selectCustomer(customer: CustomerResponse) {
    setFoundCustomer(customer)
    setSearchQuery(customer.name)
    setShowDropdown(false)
    setCreatingNew(false)
  }

  function clearCustomer() {
    setFoundCustomer(undefined)
    setSearchQuery('')
    setDebouncedQuery('')
    setShowDropdown(false)
    setCreatingNew(false)
  }

  function startCreatingNew() {
    setFoundCustomer(null)
    setCreatingNew(true)
    setShowDropdown(false)
  }

  const createMutation = useMutation({
    mutationFn: async (values: FormValues) => {
      let customerId: string

      if (foundCustomer) {
        customerId = foundCustomer.id
      } else {
        const name = values.newCustomerName?.trim()
        const phone = values.newCustomerPhone?.trim()
        if (!name) throw new Error('MISSING_NAME')
        if (!phone) throw new Error('MISSING_PHONE')
        const created = await agendaService.createCustomer({ name, phoneNumber: phone })
        customerId = created.id
      }

      if (values.repeatType !== 'none') {
        return agendaService.createRecurringSchedule({
          customerId,
          professionalId: values.professionalId,
          serviceId: values.serviceId,
          startDateTime: values.startDateTime,
          repeatType: values.repeatType,
          repeatCount: values.indefinite ? undefined : values.repeatCount,
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
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['schedules'] })
      if (watch('repeatType') !== 'none' && Array.isArray(result)) {
        appToast.success(`Série recorrente criada: ${result.length} agendamentos!`)
      } else {
        appToast.success('Agendamento criado com sucesso!')
      }
      onClose()
    },
    onError: (err: unknown) => {
      if ((err as Error).message === 'MISSING_NAME') {
        appToast.error('Informe o nome do cliente.')
        return
      }
      if ((err as Error).message === 'MISSING_PHONE') {
        appToast.error('Informe o telefone do cliente.')
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
              <div className="grid grid-cols-3 sm:grid-cols-4 gap-2 max-h-36 overflow-y-auto overscroll-contain pr-0.5">
                {slotsData.slots.map((slot) => {
                  const label = fmtTime(slot)
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
                    const label = fmtTime(alt)
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

        {/* ── Customer section ──────────────────────────────────────────────── */}
        <div className="flex flex-col gap-3">
          <p className="text-sm font-medium text-slate-300">Cliente</p>

          {/* Selected customer card */}
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
                onClick={clearCustomer}
                className="text-slate-500 hover:text-white transition-colors"
                aria-label="Remover cliente selecionado"
              >
                <X className="h-4 w-4" aria-hidden="true" />
              </button>
            </div>
          )}

          {/* Search input with autocomplete */}
          {!foundCustomer && !creatingNew && (
            <div className="relative" ref={searchRef}>
              <div className="relative flex items-center">
                <Search className="absolute left-3.5 h-4 w-4 text-slate-500 pointer-events-none" aria-hidden="true" />
                <input
                  type="text"
                  value={searchQuery}
                  onChange={(e) => {
                    setSearchQuery(e.target.value)
                    setShowDropdown(true)
                  }}
                  onFocus={() => { if (debouncedQuery.length >= 2) setShowDropdown(true) }}
                  placeholder="Buscar por nome ou telefone..."
                  className="w-full rounded-xl border border-white/10 bg-white/5 pl-10 pr-10 py-3.5 text-sm text-white placeholder:text-slate-600 focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent hover:border-white/20 transition-all"
                />
                {searchFetching && (
                  <div className="absolute right-3.5 h-4 w-4 border-2 border-brand-500/30 border-t-brand-500 rounded-full animate-spin" />
                )}
              </div>

              {/* Dropdown */}
              {showDropdown && debouncedQuery.length >= 2 && (
                <div className="absolute top-full left-0 right-0 z-50 mt-1 rounded-xl border border-white/10 bg-[#0f172a] shadow-2xl overflow-hidden">
                  {searchResults.length > 0 && (
                    <ul>
                      {searchResults.map((c) => (
                        <li key={c.id}>
                          <button
                            type="button"
                            onMouseDown={(e) => e.preventDefault()}
                            onClick={() => selectCustomer(c)}
                            className="w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-white/5 transition-colors"
                          >
                            <div className="h-8 w-8 rounded-lg bg-brand-500/20 flex items-center justify-center flex-shrink-0">
                              <span className="text-xs font-bold text-brand-400">
                                {c.name[0]?.toUpperCase() ?? '?'}
                              </span>
                            </div>
                            <div className="flex-1 min-w-0">
                              <p className="text-sm font-medium text-white truncate">{c.name}</p>
                              <p className="text-xs text-slate-400">{c.phoneNumber}</p>
                            </div>
                          </button>
                        </li>
                      ))}
                    </ul>
                  )}

                  {searchResults.length === 0 && !searchFetching && (
                    <p className="px-4 py-3 text-sm text-slate-500">
                      Nenhum cliente encontrado para "{debouncedQuery}".
                    </p>
                  )}

                  <div className="border-t border-white/10">
                    <button
                      type="button"
                      onMouseDown={(e) => e.preventDefault()}
                      onClick={startCreatingNew}
                      className="w-full flex items-center gap-2 px-4 py-3 text-sm text-brand-400 hover:bg-white/5 transition-colors"
                    >
                      <UserPlus className="h-4 w-4" aria-hidden="true" />
                      Criar novo cliente
                    </button>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Create new customer form */}
          {creatingNew && (
            <div className="flex flex-col gap-3 rounded-xl border border-white/10 bg-white/3 p-4">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <UserPlus className="h-4 w-4 text-slate-400" aria-hidden="true" />
                  <p className="text-sm text-slate-400">Novo cliente</p>
                </div>
                <button
                  type="button"
                  onClick={clearCustomer}
                  className="text-slate-500 hover:text-white transition-colors"
                  aria-label="Cancelar"
                >
                  <X className="h-4 w-4" aria-hidden="true" />
                </button>
              </div>
              <Input
                {...register('newCustomerName')}
                id="newCustomerName"
                label="Nome"
                placeholder="Nome completo"
                error={errors.newCustomerName?.message}
              />
              <Input
                {...register('newCustomerPhone')}
                id="newCustomerPhone"
                type="tel"
                label="Telefone"
                placeholder="+55 11 99999-9999"
                error={errors.newCustomerPhone?.message}
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
          <Controller
            name="repeatType"
            control={control}
            render={({ field }) => (
              <Select
                {...field}
                id="repeatType"
                label="Repetição"
                options={[
                  { value: 'none', label: 'Não repetir' },
                  { value: 'weekly', label: 'Semanal' },
                  { value: 'monthly', label: 'Mensal' },
                ]}
              />
            )}
          />

          {watchedRepeatType !== 'none' && (
            <>
              <label className="flex items-center gap-3 cursor-pointer select-none">
                <input
                  type="checkbox"
                  {...register('indefinite')}
                  className="h-4 w-4 rounded border-white/20 bg-white/5 accent-brand-500"
                />
                <span className="text-sm text-slate-300">
                  Prazo indeterminado (cria para 5 anos)
                </span>
              </label>

              {!watchedIndefinite && (
                <Input
                  {...register('repeatCount', { valueAsNumber: true })}
                  id="repeatCount"
                  type="number"
                  label={watchedRepeatType === 'monthly' ? 'Número de meses (máx. 60)' : 'Número de semanas (máx. 260)'}
                  min={1}
                  max={watchedRepeatType === 'monthly' ? 60 : 260}
                  error={errors.repeatCount?.message}
                />
              )}
            </>
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
                    <li key={d}>{fmtDateTime(d)}</li>
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
            {watchedRepeatType !== 'none' ? 'Criar série recorrente' : 'Criar agendamento'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
