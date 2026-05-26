import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Clock, FileText, Phone, Scissors, User } from 'lucide-react'
import { Modal } from '@/shared/components/ui/Modal'
import { Button } from '@/shared/components/ui/Button'
import { Badge } from '@/shared/components/ui/Badge'
import { appToast } from '@/shared/lib/toast'
import { agendaService } from '@/features/agenda/services/agenda.service'
import {
  ScheduleStatus,
  type ProfessionalResponse,
  type ScheduleResponse,
  type ScheduleStatusValue,
  type ServiceCatalogResponse,
} from '@/features/agenda/types/agenda.types'
import { fmtDateTime, fmtTime } from '@/shared/utils/date'

// ── Helpers ────────────────────────────────────────────────────────────────────

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

function toDateInputValue(iso: string): string {
  return iso.split('T')[0]
}

// ── Props ──────────────────────────────────────────────────────────────────────

export interface EventDetailModalProps {
  schedule: ScheduleResponse
  professional: ProfessionalResponse | undefined
  service: ServiceCatalogResponse | undefined
  onClose: () => void
}

// ── Component ──────────────────────────────────────────────────────────────────

export function EventDetailModal({ schedule, professional, service, onClose }: EventDetailModalProps) {
  const queryClient = useQueryClient()
  const [rescheduleMode, setRescheduleMode] = useState(false)
  const [selectedDate, setSelectedDate] = useState(toDateInputValue(schedule.startDateTime))
  const [selectedSlot, setSelectedSlot] = useState<string | null>(null)

  // Customer info
  const { data: customer } = useQuery({
    queryKey: ['customer', schedule.customerId],
    queryFn: () => agendaService.getCustomerById(schedule.customerId),
    staleTime: 5 * 60 * 1000,
  })

  // Available slots for reschedule
  const { data: slotsData, isFetching: loadingSlots } = useQuery({
    queryKey: ['slots', professional?.id, service?.id, selectedDate],
    queryFn: () => agendaService.getAvailableSlots(professional!.id, service!.id, selectedDate),
    enabled: rescheduleMode && !!professional && !!service && !!selectedDate,
    staleTime: 0,
  })

  const updateStatusMutation = useMutation({
    mutationFn: (status: ScheduleStatusValue) => agendaService.updateStatus(schedule.id, status),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schedules'] })
      appToast.success('Status atualizado.')
      onClose()
    },
    onError: (err: unknown) => appToast.error(appToast.apiError(err, 'Erro ao atualizar.')),
  })

  const rescheduleMutation = useMutation({
    mutationFn: (startDateTime: string) =>
      agendaService.updateSchedule(schedule.id, { startDateTime }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schedules'] })
      appToast.success('Agendamento reagendado.')
      onClose()
    },
    onError: (err: unknown) => appToast.error(appToast.apiError(err, 'Erro ao reagendar.')),
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

  const isBusy =
    updateStatusMutation.isPending || deleteMutation.isPending || rescheduleMutation.isPending

  const isReadOnly =
    schedule.status === ScheduleStatus.Completed ||
    schedule.status === ScheduleStatus.Cancelled ||
    schedule.status === ScheduleStatus.NoShow

  return (
    <Modal isOpen onClose={onClose} title="Detalhes do agendamento" size="md">
      <div className="flex flex-col gap-4">
        {/* Status */}
        <div className="flex items-center justify-between">
          <span className="text-sm text-slate-400">Status</span>
          <Badge variant={STATUS_VARIANT[schedule.status]}>{STATUS_LABEL[schedule.status]}</Badge>
        </div>

        {/* Details */}
        <div className="flex flex-col gap-3 py-1">
          {customer && (
            <div className="flex items-center gap-3 text-sm">
              <User className="h-4 w-4 text-slate-500 flex-shrink-0" aria-hidden="true" />
              <div className="min-w-0">
                <span className="text-slate-200 font-medium">{customer.name}</span>
                {customer.phoneNumber && (
                  <div className="flex items-center gap-1 mt-0.5">
                    <Phone className="h-3 w-3 text-slate-600" aria-hidden="true" />
                    <span className="text-xs text-slate-500">{customer.phoneNumber}</span>
                  </div>
                )}
              </div>
            </div>
          )}

          {professional && (
            <div className="flex items-center gap-3 text-sm">
              <div
                className="h-4 w-4 rounded-full flex-shrink-0 ring-1 ring-white/20"
                style={{ backgroundColor: professional.calendarColor ?? '#0ea5e9' }}
                aria-hidden="true"
              />
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
              {fmtDateTime(schedule.startDateTime)} → {fmtTime(schedule.endDateTime)}
            </span>
          </div>

          {schedule.notes && (
            <div className="flex items-start gap-3 text-sm">
              <FileText className="h-4 w-4 text-slate-500 flex-shrink-0 mt-0.5" aria-hidden="true" />
              <span className="text-slate-300">{schedule.notes}</span>
            </div>
          )}
        </div>

        {/* ── Reschedule panel ───────────────────────────────────────────────── */}
        {rescheduleMode && (
          <div className="flex flex-col gap-3 border-t border-white/10 pt-4">
            <p className="text-sm font-medium text-slate-300">Selecione nova data e horário</p>

            <input
              type="date"
              value={selectedDate}
              min={new Date().toISOString().split('T')[0]}
              onChange={(e) => {
                setSelectedDate(e.target.value)
                setSelectedSlot(null)
              }}
              className="field-base"
            />

            {loadingSlots && (
              <p className="text-xs text-slate-500 text-center py-2">Carregando horários...</p>
            )}

            {!loadingSlots && slotsData && slotsData.slots.length === 0 && (
              <p className="text-xs text-slate-500 text-center py-2">
                Nenhum horário disponível nesta data.
              </p>
            )}

            {!loadingSlots && slotsData && slotsData.slots.length > 0 && (
              <div className="grid grid-cols-3 gap-2">
                {slotsData.slots.map((slot) => {
                  const label = fmtTime(slot)
                  const isSelected = slot === selectedSlot
                  return (
                    <button
                      key={slot}
                      type="button"
                      onClick={() => setSelectedSlot(slot)}
                      className={`rounded-lg py-2 text-sm font-medium transition-colors ${
                        isSelected
                          ? 'bg-brand-500 text-white'
                          : 'bg-white/5 text-slate-300 hover:bg-white/10 border border-white/10'
                      }`}
                    >
                      {label}
                    </button>
                  )
                })}
              </div>
            )}

            <div className="flex gap-2">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => {
                  setRescheduleMode(false)
                  setSelectedSlot(null)
                }}
                disabled={isBusy}
                className="flex-1"
              >
                Voltar
              </Button>
              <Button
                size="sm"
                onClick={() => selectedSlot && rescheduleMutation.mutate(selectedSlot)}
                isLoading={rescheduleMutation.isPending}
                disabled={!selectedSlot || isBusy}
                className="flex-1"
              >
                Confirmar
              </Button>
            </div>
          </div>
        )}

        {/* ── Actions ────────────────────────────────────────────────────────── */}
        {!rescheduleMode && !isReadOnly && (
          <div className="flex flex-wrap gap-2 pt-2 border-t border-white/10">
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
              variant="ghost"
              size="sm"
              onClick={() => setRescheduleMode(true)}
              disabled={isBusy}
              className="flex-1"
            >
              Reagendar
            </Button>

            {schedule.status === ScheduleStatus.Pending && (
              <Button
                size="sm"
                onClick={() => updateStatusMutation.mutate(ScheduleStatus.Confirmed)}
                isLoading={updateStatusMutation.isPending}
                disabled={isBusy}
                className="flex-1"
              >
                Confirmar
              </Button>
            )}
          </div>
        )}
      </div>
    </Modal>
  )
}
