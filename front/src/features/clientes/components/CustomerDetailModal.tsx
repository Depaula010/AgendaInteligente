import { useQuery } from '@tanstack/react-query'
import { Calendar, Mail, Phone } from 'lucide-react'
import { Modal } from '@/shared/components/ui/Modal'
import { Badge } from '@/shared/components/ui/Badge'
import { agendaService } from '@/features/agenda/services/agenda.service'
import { clientesService } from '@/features/clientes/services/clientes.service'
import {
  ScheduleStatus,
  type CustomerResponse,
} from '@/features/agenda/types/agenda.types'

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

function fmtDate(iso: string) {
  return new Date(iso).toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    timeZone: 'UTC',
  })
}

function fmtDateOnly(iso: string) {
  return new Date(iso).toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    timeZone: 'UTC',
  })
}

// ── Props ──────────────────────────────────────────────────────────────────────

interface CustomerDetailModalProps {
  customer: CustomerResponse
  onClose: () => void
}

// ── Component ──────────────────────────────────────────────────────────────────

export function CustomerDetailModal({ customer, onClose }: CustomerDetailModalProps) {
  const { data: schedules = [], isLoading: loadingSchedules } = useQuery({
    queryKey: ['customer-schedules', customer.id],
    queryFn: () => clientesService.getCustomerSchedules(customer.id),
    staleTime: 60_000,
  })

  const { data: services = [] } = useQuery({
    queryKey: ['services'],
    queryFn: agendaService.getServices,
    staleTime: 5 * 60 * 1000,
  })

  const initial = customer.name[0]?.toUpperCase() ?? '?'

  return (
    <Modal isOpen onClose={onClose} title="Detalhes do cliente" size="lg">
      <div className="flex flex-col gap-5">
        {/* Customer header */}
        <div className="flex items-center gap-4">
          <div className="h-14 w-14 rounded-2xl bg-brand-500/20 flex items-center justify-center flex-shrink-0">
            <span className="text-xl font-bold text-brand-400">{initial}</span>
          </div>
          <div className="flex-1 min-w-0">
            <h3 className="text-base font-semibold text-white truncate">{customer.name}</h3>
            <div className="flex flex-col gap-0.5 mt-1">
              <div className="flex items-center gap-1.5 text-sm text-slate-400">
                <Phone className="h-3.5 w-3.5 flex-shrink-0" aria-hidden="true" />
                <span>{customer.phoneNumber}</span>
              </div>
              {customer.email && (
                <div className="flex items-center gap-1.5 text-sm text-slate-400">
                  <Mail className="h-3.5 w-3.5 flex-shrink-0" aria-hidden="true" />
                  <span className="truncate">{customer.email}</span>
                </div>
              )}
              {customer.lastVisitAt && (
                <div className="flex items-center gap-1.5 text-xs text-slate-500 mt-0.5">
                  <Calendar className="h-3 w-3 flex-shrink-0" aria-hidden="true" />
                  <span>Última visita: {fmtDateOnly(customer.lastVisitAt)}</span>
                </div>
              )}
            </div>
          </div>
        </div>

        <div className="h-px bg-white/10" />

        {/* Schedule history */}
        <div className="flex flex-col gap-3">
          <p className="text-sm font-semibold text-slate-300 uppercase tracking-wider text-xs">
            Histórico de agendamentos
          </p>

          {loadingSchedules && (
            <div className="flex flex-col gap-2">
              {[0, 1, 2].map((i) => (
                <div
                  key={i}
                  className="h-14 rounded-xl bg-white/5 animate-pulse"
                />
              ))}
            </div>
          )}

          {!loadingSchedules && schedules.length === 0 && (
            <p className="text-sm text-slate-500 text-center py-6">
              Nenhum agendamento encontrado.
            </p>
          )}

          {!loadingSchedules && schedules.length > 0 && (
            <div className="flex flex-col gap-2 max-h-72 overflow-y-auto custom-scrollbar">
              {schedules.map((s) => {
                const service = services.find((sv) => sv.id === s.serviceId)
                return (
                  <div
                    key={s.id}
                    className="flex items-center justify-between gap-3 rounded-xl border border-white/8 bg-white/3 px-4 py-3"
                  >
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-white truncate">
                        {service?.name ?? 'Agendamento'}
                      </p>
                      <p className="text-xs text-slate-400 mt-0.5">{fmtDate(s.startDateTime)}</p>
                    </div>
                    <Badge variant={STATUS_VARIANT[s.status]}>{STATUS_LABEL[s.status]}</Badge>
                  </div>
                )
              })}
            </div>
          )}
        </div>
      </div>
    </Modal>
  )
}
