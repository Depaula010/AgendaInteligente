import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Calendar, Mail, Phone, Pencil, Check, X } from 'lucide-react'
import { Modal } from '@/shared/components/ui/Modal'
import { Badge } from '@/shared/components/ui/Badge'
import { agendaService } from '@/features/agenda/services/agenda.service'
import { clientesService } from '@/features/clientes/services/clientes.service'
import { appToast } from '@/shared/lib/toast'
import {
  ScheduleStatus,
  type CustomerResponse,
} from '@/features/agenda/types/agenda.types'
import { fmtDateTime, fmtDateShort } from '@/shared/utils/date'

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

// ── Inline edit field ──────────────────────────────────────────────────────────

interface InlineEditProps {
  value: string
  onSave: (v: string) => void
  onCancel: () => void
  isPending: boolean
  inputMode?: React.HTMLAttributes<HTMLInputElement>['inputMode']
}

function InlineEdit({ value: initial, onSave, onCancel, isPending, inputMode }: InlineEditProps) {
  const [val, setVal] = useState(initial)

  function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'Enter') onSave(val)
    if (e.key === 'Escape') onCancel()
  }

  return (
    <div className="flex items-center gap-1.5 flex-1 min-w-0">
      <input
        autoFocus
        value={val}
        onChange={(e) => setVal(e.target.value)}
        onKeyDown={handleKeyDown}
        inputMode={inputMode}
        className="flex-1 min-w-0 rounded-lg border border-brand-500/60 bg-white/5 px-2 py-1 text-sm font-medium text-white focus:outline-none focus:ring-1 focus:ring-brand-500"
      />
      <button
        type="button"
        onClick={() => onSave(val)}
        disabled={isPending}
        className="flex-shrink-0 flex items-center justify-center h-6 w-6 rounded text-emerald-400 hover:bg-emerald-400/10 transition-colors"
        aria-label="Confirmar"
      >
        <Check className="h-3.5 w-3.5" />
      </button>
      <button
        type="button"
        onClick={onCancel}
        className="flex-shrink-0 flex items-center justify-center h-6 w-6 rounded text-slate-400 hover:bg-white/10 transition-colors"
        aria-label="Cancelar"
      >
        <X className="h-3.5 w-3.5" />
      </button>
    </div>
  )
}

// ── Props ──────────────────────────────────────────────────────────────────────

interface CustomerDetailModalProps {
  customer: CustomerResponse
  onClose: () => void
}

// ── Component ──────────────────────────────────────────────────────────────────

export function CustomerDetailModal({ customer, onClose }: CustomerDetailModalProps) {
  const queryClient = useQueryClient()

  const [displayName, setDisplayName]   = useState(customer.name)
  const [displayPhone, setDisplayPhone] = useState(customer.phoneNumber)
  const [editingField, setEditingField] = useState<'name' | 'phone' | null>(null)

  const mutation = useMutation({
    mutationFn: (patch: { name?: string; phoneNumber?: string }) =>
      clientesService.updateCustomer(customer.id, patch),
    onSuccess: (updated) => {
      queryClient.invalidateQueries({ queryKey: ['customers'] })
      setDisplayName(updated.name)
      setDisplayPhone(updated.phoneNumber)
      setEditingField(null)
      appToast.success('Cliente atualizado.')
    },
    onError: (err: unknown) => appToast.error(appToast.apiError(err, 'Erro ao atualizar cliente.')),
  })

  function saveName(raw: string) {
    const trimmed = raw.trim()
    if (!trimmed || trimmed === displayName) { setEditingField(null); return }
    mutation.mutate({ name: trimmed })
  }

  function savePhone(raw: string) {
    const trimmed = raw.trim()
    if (!trimmed || trimmed === displayPhone) { setEditingField(null); return }
    mutation.mutate({ phoneNumber: trimmed })
  }

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

  const initial = displayName[0]?.toUpperCase() ?? '?'

  return (
    <Modal isOpen onClose={onClose} title="Detalhes do cliente" size="lg">
      <div className="flex flex-col gap-5">
        {/* Customer header */}
        <div className="flex items-center gap-4">
          <div className="h-14 w-14 rounded-2xl bg-brand-500/20 flex items-center justify-center flex-shrink-0">
            <span className="text-xl font-bold text-brand-400">{initial}</span>
          </div>

          <div className="flex-1 min-w-0 flex flex-col gap-1">
            {/* Name row */}
            {editingField === 'name' ? (
              <InlineEdit
                value={displayName}
                onSave={saveName}
                onCancel={() => setEditingField(null)}
                isPending={mutation.isPending}
              />
            ) : (
              <div className="flex items-center gap-2">
                <h3 className="text-base font-semibold text-white truncate">{displayName}</h3>
                <button
                  type="button"
                  onClick={() => setEditingField('name')}
                  className="flex-shrink-0 flex items-center justify-center h-5 w-5 rounded text-slate-500 hover:text-slate-300 hover:bg-white/10 transition-colors"
                  aria-label="Editar nome"
                >
                  <Pencil className="h-3 w-3" />
                </button>
              </div>
            )}

            {/* Phone row */}
            {editingField === 'phone' ? (
              <InlineEdit
                value={displayPhone}
                onSave={savePhone}
                onCancel={() => setEditingField(null)}
                isPending={mutation.isPending}
                inputMode="tel"
              />
            ) : (
              <div className="flex items-center gap-1.5">
                <Phone className="h-3.5 w-3.5 flex-shrink-0 text-slate-400" aria-hidden="true" />
                <span className="text-sm text-slate-400">{displayPhone}</span>
                <button
                  type="button"
                  onClick={() => setEditingField('phone')}
                  className="flex-shrink-0 flex items-center justify-center h-5 w-5 rounded text-slate-500 hover:text-slate-300 hover:bg-white/10 transition-colors"
                  aria-label="Editar telefone"
                >
                  <Pencil className="h-3 w-3" />
                </button>
              </div>
            )}

            {/* Email (read-only) */}
            {customer.email && (
              <div className="flex items-center gap-1.5 text-sm text-slate-400">
                <Mail className="h-3.5 w-3.5 flex-shrink-0" aria-hidden="true" />
                <span className="truncate">{customer.email}</span>
              </div>
            )}

            {/* Last visit */}
            {customer.lastVisitAt && (
              <div className="flex items-center gap-1.5 text-xs text-slate-500">
                <Calendar className="h-3 w-3 flex-shrink-0" aria-hidden="true" />
                <span>Última visita: {fmtDateShort(customer.lastVisitAt)}</span>
              </div>
            )}
          </div>
        </div>

        <div className="h-px bg-white/10" />

        {/* Schedule history */}
        <div className="flex flex-col gap-3">
          <p className="text-xs font-semibold text-slate-300 uppercase tracking-wider">
            Histórico de agendamentos
          </p>

          {loadingSchedules && (
            <div className="flex flex-col gap-2">
              {[0, 1, 2].map((i) => (
                <div key={i} className="h-14 rounded-xl bg-white/5 animate-pulse" />
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
                      <p className="text-xs text-slate-400 mt-0.5">{fmtDateTime(s.startDateTime)}</p>
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
