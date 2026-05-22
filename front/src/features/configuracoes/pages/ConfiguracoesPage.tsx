import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Bot, BrainCircuit, Clock, Info, Loader2, Lock, Settings, X } from 'lucide-react'
import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { appToast } from '@/shared/lib/toast'
import { useAuthStore } from '@/features/auth/store/authStore'
import { configuracoesService } from '@/features/configuracoes/services/configuracoes.service'
import type { WorkingHourEntry, SaveTenantSettingsRequest } from '@/features/configuracoes/types/configuracoes.types'

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
  openTime: '09:00',
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
  const isOwner = role === 'Owner'
  const queryClient = useQueryClient()

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
    })
  }, [settings, reset])

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
      <div className="flex-1 flex items-center justify-center p-8">
        <Loader2 className="h-8 w-8 text-brand-400 animate-spin" aria-label="Carregando" />
      </div>
    )
  }

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

      {/* Staff banner */}
      {!isOwner && (
        <div className="flex items-center gap-3 rounded-xl border border-amber-500/30 bg-amber-500/10 px-4 py-3 mb-6">
          <Lock className="h-4 w-4 text-amber-400 flex-shrink-0" aria-hidden="true" />
          <p className="text-sm text-amber-300">
            Apenas o proprietário pode alterar as configurações.
          </p>
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-5">

        {/* ── Horário de funcionamento ── */}
        <SectionCard icon={Clock} title="Horário de funcionamento">
          <div className="flex flex-col gap-2">
            {days.map((day, i) => (
              <div
                key={i}
                className={`flex items-center gap-3 rounded-xl px-3 py-2.5 transition-colors ${
                  day.enabled ? 'bg-white/5' : 'opacity-50'
                }`}
              >
                {/* Toggle */}
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
                {/* Day name */}
                <label
                  htmlFor={`day-${i}`}
                  className="w-20 text-sm font-medium text-slate-300 cursor-pointer select-none"
                >
                  {DAY_NAMES[i]}
                </label>

                {/* Times — only show when enabled */}
                {day.enabled && (
                  <div className="flex items-center gap-2 flex-1">
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
          <div className="grid grid-cols-2 gap-4">
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
