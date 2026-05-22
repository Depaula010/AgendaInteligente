import { useMemo, useRef, useState } from 'react'
import FullCalendar from '@fullcalendar/react'
import dayGridPlugin from '@fullcalendar/daygrid'
import timeGridPlugin from '@fullcalendar/timegrid'
import interactionPlugin from '@fullcalendar/interaction'
import type { DateSelectArg, EventClickArg, DatesSetArg } from '@fullcalendar/core'
import ptBrLocale from '@fullcalendar/core/locales/pt-br'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { X, Clock, User, Scissors, FileText, Loader2 } from 'lucide-react'
import { cn } from '@/shared/utils/cn'
import { agendaService } from '@/features/agenda/services/agenda.service'
import { ScheduleStatus, type ScheduleStatusValue, type ScheduleResponse, type ProfessionalResponse, type ServiceCatalogResponse } from '@/features/agenda/types/agenda.types'
import { Button } from '@/shared/components/ui/Button'
import { Badge } from '@/shared/components/ui/Badge'
import { appToast } from '@/shared/lib/toast'

// ── Status helpers ──────────────────────────────────────────────────────────

const STATUS_LABEL: Record<number, string> = {
  [ScheduleStatus.Pending]: 'Pendente',
  [ScheduleStatus.Confirmed]: 'Confirmado',
  [ScheduleStatus.Cancelled]: 'Cancelado',
  [ScheduleStatus.Completed]: 'Concluído',
  [ScheduleStatus.NoShow]: 'Não compareceu',
}

const STATUS_VARIANT: Record<number, 'default' | 'warning' | 'success' | 'danger' | 'info'> = {
  [ScheduleStatus.Pending]: 'warning',
  [ScheduleStatus.Confirmed]: 'success',
  [ScheduleStatus.Cancelled]: 'danger',
  [ScheduleStatus.Completed]: 'info',
  [ScheduleStatus.NoShow]: 'default',
}

const DEFAULT_COLOR = '#0ea5e9'

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

// ── EventDetailModal ─────────────────────────────────────────────────────────

interface EventDetailModalProps {
  schedule: ScheduleResponse
  professional: ProfessionalResponse | undefined
  service: ServiceCatalogResponse | undefined
  onClose: () => void
}

function EventDetailModal({ schedule, professional, service, onClose }: EventDetailModalProps) {
  const queryClient = useQueryClient()

  const updateMutation = useMutation({
    mutationFn: (status: ScheduleStatusValue) => agendaService.updateStatus(schedule.id, status),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schedules'] })
      appToast.success('Status atualizado.')
      onClose()
    },
    onError: (err: unknown) => appToast.error(appToast.apiError(err, 'Erro ao atualizar.')),
  })

  const deleteMutation = useMutation({
    mutationFn: () => agendaService.deleteSchedule(schedule.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schedules'] })
      appToast.success('Agendamento cancelado.')
      onClose()
    },
    onError: (err: unknown) => appToast.error(appToast.apiError(err, 'Erro ao cancelar.')),
  })

  const isBusy = updateMutation.isPending || deleteMutation.isPending

  return (
    <div
      className="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4"
      aria-modal="true"
      role="dialog"
      aria-label="Detalhes do agendamento"
    >
      <div
        className="absolute inset-0 bg-black/60 backdrop-blur-sm"
        onClick={onClose}
        aria-hidden="true"
      />

      <div className="relative w-full sm:max-w-md rounded-t-3xl sm:rounded-2xl bg-surface-800 border border-white/10 shadow-glass animate-slide-up">
        {/* Header */}
        <div className="flex items-start justify-between p-5 pb-3">
          <div>
            <h2 className="font-display text-lg font-bold text-white">Agendamento</h2>
            <p className="text-xs text-slate-500 mt-0.5">{formatDateTime(schedule.startDateTime)}</p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="flex items-center justify-center h-8 w-8 rounded-lg text-slate-400 hover:text-white hover:bg-white/10 transition-colors"
            aria-label="Fechar"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        <div className="h-px bg-white/10 mx-5" />

        <div className="p-5 flex flex-col gap-4">
          {/* Status */}
          <div className="flex items-center justify-between">
            <span className="text-sm text-slate-400">Status</span>
            <Badge variant={STATUS_VARIANT[schedule.status]}>
              {STATUS_LABEL[schedule.status]}
            </Badge>
          </div>

          {/* Details */}
          <div className="flex flex-col gap-3">
            {professional && (
              <div className="flex items-center gap-3 text-sm">
                <User className="h-4 w-4 text-slate-500 flex-shrink-0" aria-hidden="true" />
                <span className="text-slate-300">{professional.name}</span>
              </div>
            )}
            {service && (
              <div className="flex items-center gap-3 text-sm">
                <Scissors className="h-4 w-4 text-slate-500 flex-shrink-0" aria-hidden="true" />
                <div>
                  <span className="text-slate-300">{service.name}</span>
                  <span className="text-slate-500 ml-2">
                    {service.durationMinutes} min · R$ {service.price.toFixed(2)}
                  </span>
                </div>
              </div>
            )}
            <div className="flex items-center gap-3 text-sm">
              <Clock className="h-4 w-4 text-slate-500 flex-shrink-0" aria-hidden="true" />
              <span className="text-slate-300">
                {formatDateTime(schedule.startDateTime)} → {formatDateTime(schedule.endDateTime)}
              </span>
            </div>
            {schedule.notes && (
              <div className="flex items-start gap-3 text-sm">
                <FileText className="h-4 w-4 text-slate-500 flex-shrink-0 mt-0.5" aria-hidden="true" />
                <span className="text-slate-300">{schedule.notes}</span>
              </div>
            )}
          </div>

          {/* Actions */}
          {schedule.status === ScheduleStatus.Pending && (
            <div className="flex gap-3 pt-2 border-t border-white/10">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => deleteMutation.mutate()}
                isLoading={deleteMutation.isPending}
                disabled={isBusy}
                className="flex-1 !text-red-400 hover:!bg-red-400/10"
              >
                Cancelar
              </Button>
              <Button
                size="sm"
                onClick={() => updateMutation.mutate(ScheduleStatus.Confirmed)}
                isLoading={updateMutation.isPending}
                disabled={isBusy}
                className="flex-1"
              >
                Confirmar
              </Button>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

// ── CreateScheduleHint ───────────────────────────────────────────────────────

function CreateScheduleHint({ onClose }: { onClose: () => void }) {
  return (
    <div
      className="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4"
      aria-modal="true"
      role="dialog"
    >
      <div
        className="absolute inset-0 bg-black/60 backdrop-blur-sm"
        onClick={onClose}
        aria-hidden="true"
      />
      <div className="relative w-full sm:max-w-sm rounded-t-3xl sm:rounded-2xl bg-surface-800 border border-white/10 shadow-glass animate-slide-up p-6">
        <button
          type="button"
          onClick={onClose}
          className="absolute top-4 right-4 flex items-center justify-center h-8 w-8 rounded-lg text-slate-400 hover:text-white hover:bg-white/10 transition-colors"
          aria-label="Fechar"
        >
          <X className="h-4 w-4" />
        </button>
        <h2 className="font-display text-base font-bold text-white mb-1">Novo agendamento</h2>
        <p className="text-sm text-slate-400">
          A criação manual de agendamentos estará disponível quando a tela de{' '}
          <span className="text-white font-medium">Clientes</span> (F17) for implementada.
          Por enquanto, os clientes podem agendar pelo{' '}
          <span className="text-white font-medium">bot do WhatsApp</span>.
        </p>
        <Button className="mt-4 w-full" variant="ghost" onClick={onClose}>
          Entendi
        </Button>
      </div>
    </div>
  )
}

// ── AgendaPage ───────────────────────────────────────────────────────────────

interface SelectedEvent {
  schedule: ScheduleResponse
  professional: ProfessionalResponse | undefined
  service: ServiceCatalogResponse | undefined
}

export function AgendaPage() {
  const calendarRef = useRef<FullCalendar>(null)
  const [dateRange, setDateRange] = useState({ start: '', end: '' })
  const [selectedEvent, setSelectedEvent] = useState<SelectedEvent | null>(null)
  const [showCreateHint, setShowCreateHint] = useState(false)

  const { data: schedules = [], isLoading: loadingSchedules } = useQuery({
    queryKey: ['schedules', dateRange],
    queryFn: () =>
      dateRange.start
        ? agendaService.getSchedules(dateRange.start, dateRange.end)
        : Promise.resolve([]),
    enabled: !!dateRange.start,
  })

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

  const events = useMemo(
    () =>
      schedules
        .filter((s) => s.status !== ScheduleStatus.Cancelled)
        .map((s) => {
          const professional = professionals.find((p) => p.id === s.professionalId)
          const service = services.find((sv) => sv.id === s.serviceId)
          const color =
            service?.calendarColor ?? professional?.calendarColor ?? DEFAULT_COLOR
          return {
            id: s.id,
            title: service?.name ?? 'Agendamento',
            start: s.startDateTime,
            end: s.endDateTime,
            backgroundColor: color,
            borderColor: 'transparent',
            textColor: '#ffffff',
            extendedProps: { schedule: s, professional, service },
          }
        }),
    [schedules, professionals, services],
  )

  function handleDatesSet(info: DatesSetArg) {
    setDateRange({ start: info.startStr, end: info.endStr })
  }

  function handleEventClick(info: EventClickArg) {
    const { schedule, professional, service } = info.event.extendedProps as {
      schedule: ScheduleResponse
      professional: ProfessionalResponse | undefined
      service: ServiceCatalogResponse | undefined
    }
    setSelectedEvent({ schedule, professional, service })
  }

  function handleDateSelect(_info: DateSelectArg) {
    setShowCreateHint(true)
  }

  return (
    <div className="flex flex-col h-full">
      {/* Loading indicator */}
      {loadingSchedules && (
        <div className="flex items-center gap-2 px-4 py-2 bg-brand-500/10 border-b border-brand-500/20">
          <Loader2 className="h-3 w-3 text-brand-400 animate-spin" aria-hidden="true" />
          <span className="text-xs text-brand-400">Carregando agendamentos...</span>
        </div>
      )}

      {/* FullCalendar */}
      <div
        className={cn('fc-dark flex-1 overflow-hidden', loadingSchedules && 'opacity-70')}
        style={{ minHeight: 0 }}
      >
        <FullCalendar
          ref={calendarRef}
          plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin]}
          initialView="timeGridWeek"
          headerToolbar={{
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek,timeGridDay',
          }}
          locale={ptBrLocale}
          events={events}
          selectable
          selectMirror
          select={handleDateSelect}
          eventClick={handleEventClick}
          datesSet={handleDatesSet}
          height="100%"
          slotMinTime="06:00:00"
          slotMaxTime="22:00:00"
          allDaySlot={false}
          nowIndicator
          slotDuration="00:30:00"
          eventDisplay="block"
          dayMaxEvents={3}
        />
      </div>

      {/* Modals */}
      {selectedEvent && (
        <EventDetailModal
          {...selectedEvent}
          onClose={() => setSelectedEvent(null)}
        />
      )}
      {showCreateHint && <CreateScheduleHint onClose={() => setShowCreateHint(false)} />}
    </div>
  )
}
