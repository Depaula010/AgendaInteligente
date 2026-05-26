import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Bot, BrainCircuit, CalendarX, Clock, Globe, Info, Settings, X } from 'lucide-react'
import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { Select } from '@/shared/components/ui/Select'
import { Skeleton } from '@/shared/components/ui/Skeleton'
import { appToast } from '@/shared/lib/toast'
import { useAuthStore } from '@/features/auth/store/authStore'
import { configuracoesService } from '@/features/configuracoes/services/configuracoes.service'
import type { WorkingHourEntry, SaveTenantSettingsRequest } from '@/features/configuracoes/types/configuracoes.types'
import { BRAZIL_TIMEZONES } from '@/features/configuracoes/types/configuracoes.types'
import { api } from '@/shared/lib/axios'

interface BlockoutResponse {
  id: string
  professionalId: string
  startDateTime: string
  endDateTime: string
  blockReason: string | null
  isAllDay: boolean
  createdAt: string
}

// ── Constants ──────────────────────────────────────────────────────────────────

const DAY_NAMES = ['Domingo', 'Segunda', 'Terça', 'Quarta', 'Quinta', 'Sexta', 'Sábado']

// ── Schema ─────────────────────────────────────────────────────────────────────

const schema = z.object({
  botDisplayName:           z.string().optional(),
  whatsAppPhoneNumber:      z.string().optional(),
  conflictMessageTemplate:  z.string().optional(),
  reminderLeadTimeHours:    z.number({ error: 'Informe um número' }).int().min(0, 'Mínimo 0'),
  reengagementInactiveDays: z.number({ error: 'Informe um número' }).int().min(0, 'Mínimo 0'),
  geminiModel:              z.string().min(1, 'Informe o modelo'),
  timeZoneId:               z.string().min(1, 'Selecione um fuso horário'),
})

type FormValues = z.infer<typeof schema>

// ── DayConfig ──────────────────────────────────────────────────────────────────

interface DayConfig {
  enabled: boolean
  openTime: string
  closeTime: string
}

const DEFAULT_DAYS: DayConfig[] = Array.from({ length: 7 }, () => ({
  enabled: false,
  openTime: '08:00',
  closeTime: '18:00',
}))

// ── SectionCard ────────────────────────────────────────────────────────────────

function SectionCard({
  icon: Icon,
  title,
  children,
}: {
  icon: React.ElementType
  title: string
  children: React.ReactNode
}) {
  return (
    <div className="rounded-2xl border border-white/10 bg-white/5 p-5">
      <div className="flex items-center gap-2 mb-4">
        <Icon className="h-4 w-4 text-brand-400" aria-hidden="true" />
        <h2 className="text-sm font-semibold text-slate-300 uppercase tracking-wider">{title}</h2>
      </div>
      {children}
    </div>
  )
}

// ── ConfiguracoesPage ──────────────────────────────────────────────────────────

export function ConfiguracoesPage() {
  const role    = useAuthStore((s) => s.user?.role)
  const userId  = useAuthStore((s) => s.user?.id)
  const isOwner = role === 'Owner'
  const queryClient = useQueryClient()

  // ── Staff: meus bloqueios ──
  const [newBlockoutDate,   setNewBlockoutDate]   = useState('')
  const [newBlockoutReason, setNewBlockoutReason] = useState('')

  const { data: myBlockouts = [], refetch: refetchBlockouts } = useQuery({
    queryKey: ['my-blockouts', userId],
    queryFn: async () => {
      if (!userId) return []
      const from = new Date().toISOString()
      const to   = new Date(new Date().setFullYear(new Date().getFullYear() + 2)).toISOString()
      const res  = await api.get<BlockoutResponse[]>('/schedules/block', {
        params: { professionalId: userId, from, to },
      })
      return res.data
    },
    enabled: !isOwner && !!userId,
  })

  const createBlockoutMutation = useMutation({
    mutationFn: async (date: string) => {
      await api.post('/schedules/block', {
        professionalId: userId,
        startDateTime: `${date}T00:00:00.000Z`,
        endDateTime:   `${date}T23:59:59.000Z`,
        blockReason:   newBlockoutReason || null,
        isAllDay:      true,
      })
    },
    onSuccess: () => {
      refetchBlockouts()
      setNewBlockoutDate('')
      setNewBlockoutReason('')
      appToast.success('Bloqueio adicionado.')
    },
    onError: (err: unknown) => appToast.error(appToast.apiError(err, 'Erro ao adicionar bloqueio.')),
  })

  const deleteBlockoutMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/schedules/block/${id}`),
    onSuccess: () => {
      refetchBlockouts()
      appToast.success('Bloqueio removido.')
    },
    onError: (err: unknown) => appToast.error(appToast.apiError(err, 'Erro ao remover bloqueio.')),
  })

  // ── Array state (outside RHF) ──
  const [days, setDays]       = useState<DayConfig[]>(DEFAULT_DAYS)
  const [daysOff, setDaysOff] = useState<string[]>([])
  const [newDayOff, setNewDayOff] = useState('')

  // ── Gemini key change tracking ──
  const [geminiKeyChanged, setGeminiKeyChanged] = useState(false)
  const [geminiKey, setGeminiKey]               = useState('')

  // ── RHF ──
  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      botDisplayName:           '',
      whatsAppPhoneNumber:      '',
      conflictMessageTemplate:  '',
      reminderLeadTimeHours:    24,
      reengagementInactiveDays: 30,
      geminiModel:              'gemini-2.5-flash-lite',
      timeZoneId:               'America/Sao_Paulo',
    },
  })

  // ── Load settings ──
  const { data: settings, isLoading } = useQuery({
    queryKey: ['tenant-settings'],
    queryFn: configuracoesService.get,
  })

  useEffect(() => {
    if (!settings) return

    // Parse working hours
    try {
      const parsed = JSON.parse(settings.workingHoursJson || '[]') as WorkingHourEntry[]
      setDays(
        Array.from({ length: 7 }, (_, i) => {
          const entry = parsed.find((e) => e.dayOfWeek === i)
          return {
            enabled:   !!entry,
            openTime:  entry?.openTime  ?? '09:00',
            closeTime: entry?.closeTime ?? '18:00',
          }
        }),
      )
    } catch {
      setDays(DEFAULT_DAYS)
    }

    // Parse days off
    try {
      setDaysOff(JSON.parse(settings.daysOffJson || '[]') as string[])
    } catch {
      setDaysOff([])
    }

    // Reset form scalar fields
    reset({
      botDisplayName:           settings.botDisplayName           ?? '',
      whatsAppPhoneNumber:      settings.whatsAppPhoneNumber      ?? '',
      conflictMessageTemplate:  settings.conflictMessageTemplate  ?? '',
      reminderLeadTimeHours:    settings.reminderLeadTimeHours,
      reengagementInactiveDays: settings.reengagementInactiveDays,
      geminiModel:              settings.geminiModel || 'gemini-2.5-flash-lite',
      timeZoneId:               settings.timeZoneId  || 'America/Sao_Paulo',
    })
  }, [settings, reset])

  // ── Staff: meus horários individuais (declared after settings to avoid TDZ) ──
  const [staffDays, setStaffDays] = useState<DayConfig[]>(DEFAULT_DAYS)

  const { data: myProfessional } = useQuery({
    queryKey: ['my-professional', userId],
    queryFn: async () => {
      const res = await api.get<{ workingHoursJson: string | null }>(`/professionals/${userId}`)
      return res.data
    },
    enabled: !isOwner && !!userId,
  })

  useEffect(() => {
    if (isOwner) return
    const json = myProfessional?.workingHoursJson ?? settings?.workingHoursJson
    if (!json) return
    try {
      const parsed = JSON.parse(json) as WorkingHourEntry[]
      setStaffDays(
        Array.from({ length: 7 }, (_, i) => {
          const entry = parsed.find((e) => e.dayOfWeek === i)
          return {
            enabled:   !!entry,
            openTime:  entry?.openTime  ?? '08:00',
            closeTime: entry?.closeTime ?? '18:00',
          }
        }),
      )
    } catch {
      setStaffDays(DEFAULT_DAYS)
    }
  }, [myProfessional?.workingHoursJson, settings?.workingHoursJson, isOwner])

  const saveWorkingHoursMutation = useMutation({
    mutationFn: async (workingHoursJson: string | null) => {
      await api.put(`/professionals/${userId}/working-hours`, { workingHoursJson })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-professional', userId] })
      appToast.success('Horários salvos.')
    },
    onError: (err: unknown) => appToast.error(appToast.apiError(err, 'Erro ao salvar horários.')),
  })

  function saveStaffWorkingHours() {
    const workingHoursJson = JSON.stringify(
      staffDays
        .map((d, i) => d.enabled ? { dayOfWeek: i, openTime: d.openTime, closeTime: d.closeTime } : null)
        .filter(Boolean),
    )
    saveWorkingHoursMutation.mutate(workingHoursJson)
  }

  function resetStaffWorkingHours() {
    saveWorkingHoursMutation.mutate(null)
  }

  // ── Mutation ──
  const mutation = useMutation({
    mutationFn: (payload: SaveTenantSettingsRequest) => configuracoesService.save(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tenant-settings'] })
      appToast.success('Configurações salvas.')
      setGeminiKeyChanged(false)
      setGeminiKey('')
    },
    onError: (err: unknown) => appToast.error(appToast.apiError(err, 'Erro ao salvar configurações.')),
  })

  function onSubmit(values: FormValues) {
    const workingHoursJson = JSON.stringify(
      days
        .map((d, i) =>
          d.enabled ? { dayOfWeek: i, openTime: d.openTime, closeTime: d.closeTime } : null,
        )
        .filter(Boolean),
    )

    const payload: SaveTenantSettingsRequest = {
      workingHoursJson,
      daysOffJson:              JSON.stringify(daysOff),
      reminderLeadTimeHours:    values.reminderLeadTimeHours,
      reengagementInactiveDays: values.reengagementInactiveDays,
      botDisplayName:           values.botDisplayName    || undefined,
      whatsAppPhoneNumber:      values.whatsAppPhoneNumber || undefined,
      conflictMessageTemplate:  values.conflictMessageTemplate || undefined,
      geminiModel:              values.geminiModel,
      timeZoneId:               values.timeZoneId,
      ...(geminiKeyChanged ? { geminiApiKey: geminiKey } : {}),
    }

    mutation.mutate(payload)
  }

  // ── Day off helpers ──
  function addDayOff() {
    if (!newDayOff) return
    if (daysOff.includes(newDayOff)) {
      appToast.error('Esta data já está na lista.')
      return
    }
    setDaysOff((prev) => [...prev, newDayOff].sort())
    setNewDayOff('')
  }

  function removeDayOff(date: string) {
    setDaysOff((prev) => prev.filter((d) => d !== date))
  }

  // ── Loading ──
  if (isLoading) {
    return (
      <div className="p-4 md:p-6 max-w-2xl mx-auto" aria-label="Carregando configurações">
        <div className="flex items-center justify-between mb-6">
          <div className="flex flex-col gap-2">
            <Skeleton className="h-7 w-36" />
            <Skeleton className="h-4 w-56" />
          </div>
          <Skeleton className="h-9 w-9 rounded-xl" />
        </div>
        <div className="flex flex-col gap-5">
          {[
            [2, 'w-44'], [1, 'w-36'], [2, 'w-52'], [2, 'w-40'],
          ].map(([lines, titleW], i) => (
            <div key={i} className="rounded-2xl border border-white/10 bg-white/5 p-5">
              <div className="flex items-center gap-2 mb-4">
                <Skeleton className="h-4 w-4 rounded" />
                <Skeleton className={`h-3 ${titleW as string}`} />
              </div>
              <div className="flex flex-col gap-3">
                {Array.from({ length: lines as number }).map((_, j) => (
                  <Skeleton key={j} className="h-[3.25rem] w-full rounded-xl" />
                ))}
              </div>
            </div>
          ))}
          <Skeleton className="h-12 w-full rounded-xl" />
        </div>
      </div>
    )
  }

  // ── View do Colaborador ──────────────────────────────────────────────────────
  if (!isOwner) {
    return (
      <div className="p-4 md:p-6 max-w-2xl mx-auto">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h1 className="font-display text-xl font-bold text-white">Configurações</h1>
            <p className="text-sm text-slate-500 mt-1">Horário de funcionamento e seus bloqueios de agenda.</p>
          </div>
          <div className="h-9 w-9 rounded-xl bg-brand-500/10 flex items-center justify-center">
            <Settings className="h-5 w-5 text-brand-400" aria-hidden="true" />
          </div>
        </div>

        <div className="flex flex-col gap-5">
          {/* Meus horários de trabalho — editável */}
          <SectionCard icon={Clock} title="Meus horários de trabalho">
            <div className="flex items-center justify-between mb-3">
              {myProfessional?.workingHoursJson != null ? (
                <span className="inline-flex items-center gap-1.5 rounded-full bg-brand-500/15 border border-brand-500/30 px-2.5 py-0.5 text-xs text-brand-300">
                  Personalizado
                </span>
              ) : (
                <span className="inline-flex items-center gap-1.5 rounded-full bg-white/10 border border-white/15 px-2.5 py-0.5 text-xs text-slate-400">
                  Usando horário do estabelecimento
                </span>
              )}
              {myProfessional?.workingHoursJson != null && (
                <button
                  type="button"
                  onClick={resetStaffWorkingHours}
                  disabled={saveWorkingHoursMutation.isPending}
                  className="text-xs text-slate-500 hover:text-slate-300 transition-colors disabled:opacity-40"
                >
                  Usar horário do estabelecimento
                </button>
              )}
            </div>

            <div className="flex flex-col gap-2">
              {staffDays.map((day, i) => (
                <div
                  key={i}
                  className={`flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-3 rounded-xl px-3 py-2.5 transition-colors ${
                    day.enabled ? 'bg-white/5' : 'opacity-50'
                  }`}
                >
                  <div className="flex items-center gap-3">
                    <input
                      type="checkbox"
                      id={`staff-day-${i}`}
                      checked={day.enabled}
                      onChange={(e) =>
                        setStaffDays((prev) =>
                          prev.map((d, idx) => idx === i ? { ...d, enabled: e.target.checked } : d),
                        )
                      }
                      className="h-4 w-4 rounded accent-brand-500 flex-shrink-0 cursor-pointer"
                    />
                    <label
                      htmlFor={`staff-day-${i}`}
                      className="w-20 text-sm font-medium text-slate-300 cursor-pointer select-none"
                    >
                      {DAY_NAMES[i]}
                    </label>
                  </div>

                  {day.enabled && (
                    <div className="flex items-center gap-2 pl-7 sm:pl-0 sm:flex-1">
                      <input
                        type="time"
                        value={day.openTime}
                        onChange={(e) =>
                          setStaffDays((prev) =>
                            prev.map((d, idx) => idx === i ? { ...d, openTime: e.target.value } : d),
                          )
                        }
                        className="field-base py-1.5 text-sm w-[7rem]"
                      />
                      <span className="text-xs text-slate-500">às</span>
                      <input
                        type="time"
                        value={day.closeTime}
                        onChange={(e) =>
                          setStaffDays((prev) =>
                            prev.map((d, idx) => idx === i ? { ...d, closeTime: e.target.value } : d),
                          )
                        }
                        className="field-base py-1.5 text-sm w-[7rem]"
                      />
                    </div>
                  )}
                </div>
              ))}
            </div>

            <div className="mt-4">
              <Button
                type="button"
                size="sm"
                onClick={saveStaffWorkingHours}
                isLoading={saveWorkingHoursMutation.isPending}
                className="w-full sm:w-auto"
              >
                Salvar horários
              </Button>
            </div>
          </SectionCard>

          {/* Meus bloqueios de agenda */}
          <SectionCard icon={CalendarX} title="Meus bloqueios de agenda">
            <div className="flex gap-2 mb-3 flex-col sm:flex-row">
              <input
                type="date"
                value={newBlockoutDate}
                onChange={(e) => setNewBlockoutDate(e.target.value)}
                min={new Date().toISOString().split('T')[0]}
                className="field-base flex-1 text-sm"
                aria-label="Data do bloqueio"
              />
              <input
                type="text"
                value={newBlockoutReason}
                onChange={(e) => setNewBlockoutReason(e.target.value)}
                placeholder="Motivo (opcional)"
                className="field-base flex-1 text-sm"
              />
              <Button
                type="button"
                size="sm"
                variant="ghost"
                onClick={() => newBlockoutDate && createBlockoutMutation.mutate(newBlockoutDate)}
                disabled={!newBlockoutDate || createBlockoutMutation.isPending}
                isLoading={createBlockoutMutation.isPending}
              >
                Adicionar
              </Button>
            </div>

            {myBlockouts.length === 0 ? (
              <p className="text-sm text-slate-500 text-center py-3">Nenhum bloqueio cadastrado.</p>
            ) : (
              <div className="flex flex-wrap gap-2">
                {myBlockouts.map((b) => {
                  const [y, m, d] = b.startDateTime.split('T')[0].split('-')
                  const label = b.blockReason ? `${d}/${m}/${y} — ${b.blockReason}` : `${d}/${m}/${y}`
                  return (
                    <span
                      key={b.id}
                      className="inline-flex items-center gap-1.5 rounded-full bg-white/10 border border-white/15 px-3 py-1 text-sm text-slate-300"
                    >
                      {label}
                      <button
                        type="button"
                        onClick={() => deleteBlockoutMutation.mutate(b.id)}
                        disabled={deleteBlockoutMutation.isPending}
                        className="text-slate-500 hover:text-red-400 transition-colors focus-visible:outline-none disabled:opacity-40"
                        aria-label={`Remover bloqueio ${label}`}
                      >
                        <X className="h-3 w-3" />
                      </button>
                    </span>
                  )
                })}
              </div>
            )}
          </SectionCard>
        </div>
      </div>
    )
  }

  // ── View do Proprietário ─────────────────────────────────────────────────────
  return (
    <div className="p-4 md:p-6 max-w-2xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="font-display text-xl font-bold text-white">Configurações</h1>
          <p className="text-sm text-slate-500 mt-1">Horários, bot e integrações do estabelecimento.</p>
        </div>
        <div className="h-9 w-9 rounded-xl bg-brand-500/10 flex items-center justify-center">
          <Settings className="h-5 w-5 text-brand-400" aria-hidden="true" />
        </div>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-5">

        {/* ── Fuso horário ── */}
        <SectionCard icon={Globe} title="Fuso horário">
          <Select
            {...register('timeZoneId')}
            id="timeZoneId"
            label="Fuso horário do estabelecimento"
            options={BRAZIL_TIMEZONES.map((tz) => ({ value: tz.value, label: tz.label }))}
            disabled={!isOwner}
            error={errors.timeZoneId?.message}
            hint="Afeta o cálculo de slots disponíveis e o horário dos lembretes."
          />
        </SectionCard>

        {/* ── Horário de funcionamento ── */}
        <SectionCard icon={Clock} title="Horário de funcionamento">
          <div className="flex flex-col gap-2">
            {days.map((day, i) => (
              <div
                key={i}
                className={`flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-3 rounded-xl px-3 py-2.5 transition-colors ${
                  day.enabled ? 'bg-white/5' : 'opacity-50'
                }`}
              >
                {/* Toggle + day name */}
                <div className="flex items-center gap-3">
                  <input
                    type="checkbox"
                    id={`day-${i}`}
                    checked={day.enabled}
                    disabled={!isOwner}
                    onChange={(e) =>
                      setDays((prev) =>
                        prev.map((d, idx) => (idx === i ? { ...d, enabled: e.target.checked } : d)),
                      )
                    }
                    className="h-4 w-4 rounded accent-brand-500 flex-shrink-0 cursor-pointer disabled:cursor-not-allowed"
                  />
                  <label
                    htmlFor={`day-${i}`}
                    className="w-20 text-sm font-medium text-slate-300 cursor-pointer select-none"
                  >
                    {DAY_NAMES[i]}
                  </label>
                </div>

                {/* Times — only show when enabled */}
                {day.enabled && (
                  <div className="flex items-center gap-2 pl-7 sm:pl-0 sm:flex-1">
                    <input
                      type="time"
                      value={day.openTime}
                      disabled={!isOwner}
                      onChange={(e) =>
                        setDays((prev) =>
                          prev.map((d, idx) =>
                            idx === i ? { ...d, openTime: e.target.value } : d,
                          ),
                        )
                      }
                      className="field-base py-1.5 text-sm w-[7rem] disabled:opacity-60 disabled:cursor-not-allowed"
                    />
                    <span className="text-xs text-slate-500">às</span>
                    <input
                      type="time"
                      value={day.closeTime}
                      disabled={!isOwner}
                      onChange={(e) =>
                        setDays((prev) =>
                          prev.map((d, idx) =>
                            idx === i ? { ...d, closeTime: e.target.value } : d,
                          ),
                        )
                      }
                      className="field-base py-1.5 text-sm w-[7rem] disabled:opacity-60 disabled:cursor-not-allowed"
                    />
                  </div>
                )}
              </div>
            ))}
          </div>
        </SectionCard>

        {/* ── Dias de folga ── */}
        <SectionCard icon={Clock} title="Dias de folga / Feriados">
          {isOwner && (
            <div className="flex gap-2 mb-3">
              <input
                type="date"
                value={newDayOff}
                onChange={(e) => setNewDayOff(e.target.value)}
                className="field-base flex-1 text-sm"
                aria-label="Data do dia de folga"
              />
              <Button type="button" size="sm" variant="ghost" onClick={addDayOff} disabled={!newDayOff}>
                Adicionar
              </Button>
            </div>
          )}

          {daysOff.length === 0 ? (
            <p className="text-sm text-slate-500 text-center py-3">Nenhum dia de folga cadastrado.</p>
          ) : (
            <div className="flex flex-wrap gap-2">
              {daysOff.map((date) => {
                const [y, m, d] = date.split('-')
                const label = `${d}/${m}/${y}`
                return (
                  <span
                    key={date}
                    className="inline-flex items-center gap-1.5 rounded-full bg-white/10 border border-white/15 px-3 py-1 text-sm text-slate-300"
                  >
                    {label}
                    {isOwner && (
                      <button
                        type="button"
                        onClick={() => removeDayOff(date)}
                        className="text-slate-500 hover:text-red-400 transition-colors focus-visible:outline-none"
                        aria-label={`Remover ${label}`}
                      >
                        <X className="h-3 w-3" />
                      </button>
                    )}
                  </span>
                )
              })}
            </div>
          )}
        </SectionCard>

        {/* ── Bot do WhatsApp ── */}
        <SectionCard icon={Bot} title="Bot do WhatsApp">
          <div className="flex flex-col gap-4">
            <Input
              {...register('botDisplayName')}
              id="botDisplayName"
              label="Nome do bot"
              placeholder="Ex: Barbearia do Zé"
              disabled={!isOwner}
              error={errors.botDisplayName?.message}
            />
            <Input
              {...register('whatsAppPhoneNumber')}
              id="whatsAppPhoneNumber"
              label="Número WhatsApp (E.164)"
              placeholder="+5511999998888"
              disabled={!isOwner}
              error={errors.whatsAppPhoneNumber?.message}
            />
            <div className="flex flex-col gap-1.5">
              <label htmlFor="conflictTemplate" className="text-sm font-medium text-slate-300">
                Mensagem de conflito{' '}
                <span className="text-slate-600 font-normal">(opcional)</span>
              </label>
              <textarea
                {...register('conflictMessageTemplate')}
                id="conflictTemplate"
                rows={3}
                disabled={!isOwner}
                placeholder="Ex: Esse horário está ocupado. Horários disponíveis: {alternatives}"
                className="field-base resize-none disabled:opacity-60 disabled:cursor-not-allowed"
              />
              <p className="text-xs text-slate-500 flex items-center gap-1">
                <Info className="h-3 w-3 flex-shrink-0" aria-hidden="true" />
                Use <code className="bg-white/10 px-1 rounded">{'{alternatives}'}</code> para inserir
                os horários alternativos automaticamente.
              </p>
            </div>
          </div>
        </SectionCard>

        {/* ── Lembretes automáticos ── */}
        <SectionCard icon={Clock} title="Lembretes automáticos">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input
              {...register('reminderLeadTimeHours', { valueAsNumber: true })}
              id="reminderLeadTimeHours"
              type="number"
              min={0}
              label="Lembrete antecipado (horas)"
              placeholder="24"
              disabled={!isOwner}
              error={errors.reminderLeadTimeHours?.message}
              hint="0 = desativado"
            />
            <Input
              {...register('reengagementInactiveDays', { valueAsNumber: true })}
              id="reengagementInactiveDays"
              type="number"
              min={0}
              label="Reengajamento (dias)"
              placeholder="30"
              disabled={!isOwner}
              error={errors.reengagementInactiveDays?.message}
              hint="0 = desativado"
            />
          </div>
        </SectionCard>

        {/* ── Inteligência Artificial ── */}
        <SectionCard icon={BrainCircuit} title="Inteligência Artificial (Gemini)">
          <div className="flex flex-col gap-4">
            <div className="flex flex-col gap-1.5">
              <label htmlFor="geminiApiKey" className="text-sm font-medium text-slate-300">
                Chave API
              </label>
              <input
                id="geminiApiKey"
                type="password"
                autoComplete="new-password"
                value={geminiKey}
                disabled={!isOwner}
                placeholder={
                  settings?.hasGeminiApiKey
                    ? 'Deixe em branco para manter a chave atual'
                    : 'Cole aqui a chave do Gemini API'
                }
                onChange={(e) => {
                  setGeminiKey(e.target.value)
                  setGeminiKeyChanged(true)
                }}
                className="field-base disabled:opacity-60 disabled:cursor-not-allowed"
              />
              {settings?.hasGeminiApiKey && (
                <p className="text-xs text-emerald-400 flex items-center gap-1">
                  <span className="h-1.5 w-1.5 rounded-full bg-emerald-400 inline-block" />
                  Chave configurada
                </p>
              )}
            </div>
            <Input
              {...register('geminiModel')}
              id="geminiModel"
              label="Modelo"
              placeholder="gemini-2.5-flash-lite"
              disabled={!isOwner}
              error={errors.geminiModel?.message}
              hint="Modelo Gemini usado pelo bot de agendamento."
            />
          </div>
        </SectionCard>

        {/* ── Save ── */}
        <Button
          type="submit"
          isLoading={mutation.isPending}
          disabled={!isOwner}
          className="w-full"
        >
          Salvar configurações
        </Button>
      </form>
    </div>
  )
}
