import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Edit2, Plus, Scissors, Trash2 } from 'lucide-react'
import { Button } from '@/shared/components/ui/Button'
import { Badge } from '@/shared/components/ui/Badge'
import { Modal } from '@/shared/components/ui/Modal'
import { appToast } from '@/shared/lib/toast'
import { useAuthStore } from '@/features/auth/store/authStore'
import { servicosService } from '@/features/servicos/services/servicos.service'
import { ServiceFormModal } from '@/features/servicos/components/ServiceFormModal'
import type { ServiceCatalogResponse } from '@/features/agenda/types/agenda.types'

// ── DeleteConfirmModal ─────────────────────────────────────────────────────────

function DeleteConfirmModal({
  service,
  onClose,
}: {
  service: ServiceCatalogResponse
  onClose: () => void
}) {
  const queryClient = useQueryClient()

  const mutation = useMutation({
    mutationFn: () => servicosService.remove(service.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] })
      queryClient.invalidateQueries({ queryKey: ['services-all'] })
      appToast.success('Serviço removido.')
      onClose()
    },
    onError: (err: unknown) => appToast.error(appToast.apiError(err, 'Erro ao remover serviço.')),
  })

  return (
    <Modal isOpen onClose={onClose} title="Remover serviço" size="sm">
      <p className="text-sm text-slate-300 mb-6">
        Deseja remover <span className="font-semibold text-white">{service.name}</span>? Esta ação
        não pode ser desfeita.
      </p>
      <div className="flex gap-3">
        <Button type="button" variant="ghost" onClick={onClose} className="flex-1">
          Cancelar
        </Button>
        <Button
          type="button"
          variant="danger"
          isLoading={mutation.isPending}
          onClick={() => mutation.mutate()}
          className="flex-1"
        >
          Remover
        </Button>
      </div>
    </Modal>
  )
}

// ── ServiceCard ────────────────────────────────────────────────────────────────

function ServiceCard({
  service,
  isOwner,
  onEdit,
  onDelete,
}: {
  service: ServiceCatalogResponse
  isOwner: boolean
  onEdit: () => void
  onDelete: () => void
}) {
  const priceLabel = new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL',
  }).format(service.price)

  const durationLabel =
    service.durationMinutes >= 60
      ? `${Math.floor(service.durationMinutes / 60)}h${service.durationMinutes % 60 > 0 ? ` ${service.durationMinutes % 60}min` : ''}`
      : `${service.durationMinutes} min`

  return (
    <div className="flex items-center gap-4 rounded-xl border border-white/10 bg-white/5 px-4 py-3.5">
      {/* Color dot */}
      <div
        className="h-9 w-9 rounded-xl flex-shrink-0"
        style={{ backgroundColor: service.calendarColor ?? '#0ea5e9' }}
        aria-hidden="true"
      />

      {/* Info */}
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          <p className="text-sm font-medium text-white truncate">{service.name}</p>
          <Badge variant={service.isActive ? 'success' : 'default'}>
            {service.isActive ? 'Ativo' : 'Inativo'}
          </Badge>
        </div>
        <p className="text-xs text-slate-400 mt-0.5">
          {durationLabel} &bull; {priceLabel}
        </p>
        {service.description && (
          <p className="text-xs text-slate-500 mt-0.5 truncate">{service.description}</p>
        )}
      </div>

      {/* Owner actions */}
      {isOwner && (
        <div className="flex items-center gap-1 flex-shrink-0">
          <button
            type="button"
            onClick={onEdit}
            className="p-2 rounded-lg text-slate-400 hover:text-white hover:bg-white/10 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
            aria-label={`Editar ${service.name}`}
          >
            <Edit2 className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={onDelete}
            className="p-2 rounded-lg text-slate-400 hover:text-red-400 hover:bg-red-500/10 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-500"
            aria-label={`Remover ${service.name}`}
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      )}
    </div>
  )
}

// ── ServicosPage ───────────────────────────────────────────────────────────────

export function ServicosPage() {
  const canEdit = useAuthStore(
    (s) => s.user?.role === 'Owner' || (s.user?.canManageServices ?? false),
  )

  const [formTarget, setFormTarget] = useState<ServiceCatalogResponse | null | 'new'>(null)
  const [deleteTarget, setDeleteTarget] = useState<ServiceCatalogResponse | null>(null)

  const { data: services = [], isLoading } = useQuery({
    queryKey: ['services-all'],
    queryFn: servicosService.getAll,
  })

  const activeCount = services.filter((s) => s.isActive).length

  return (
    <div className="p-4 md:p-6 max-w-3xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="font-display text-xl font-bold text-white">Serviços</h1>
          {services.length > 0 && (
            <p className="text-sm text-slate-500 mt-0.5">
              {activeCount} ativo{activeCount !== 1 ? 's' : ''} de {services.length}
            </p>
          )}
        </div>
        <div className="flex items-center gap-2">
          {canEdit && (
            <Button size="sm" onClick={() => setFormTarget('new')}>
              <Plus className="h-4 w-4 mr-1" />
              Novo serviço
            </Button>
          )}
          {!canEdit && (
            <div className="h-9 w-9 rounded-xl bg-brand-500/10 flex items-center justify-center">
              <Scissors className="h-5 w-5 text-brand-400" aria-hidden="true" />
            </div>
          )}
        </div>
      </div>

      {/* List */}
      {isLoading ? (
        <div className="flex flex-col gap-2">
          {[0, 1, 2, 3].map((i) => (
            <div key={i} className="h-[68px] rounded-xl bg-white/5 animate-pulse" />
          ))}
        </div>
      ) : services.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <Scissors className="h-10 w-10 text-slate-700 mb-3" aria-hidden="true" />
          <p className="text-white font-medium">Nenhum serviço cadastrado</p>
          {canEdit && (
            <p className="text-sm text-slate-500 mt-1">
              Clique em "Novo serviço" para adicionar o primeiro.
            </p>
          )}
        </div>
      ) : (
        <div className="flex flex-col gap-2">
          {services.map((service) => (
            <ServiceCard
              key={service.id}
              service={service}
              isOwner={canEdit}
              onEdit={() => setFormTarget(service)}
              onDelete={() => setDeleteTarget(service)}
            />
          ))}
        </div>
      )}

      {/* Create / Edit modal */}
      {formTarget !== null && (
        <ServiceFormModal
          service={formTarget === 'new' ? undefined : formTarget}
          onClose={() => setFormTarget(null)}
        />
      )}

      {/* Delete confirm modal */}
      {deleteTarget && (
        <DeleteConfirmModal service={deleteTarget} onClose={() => setDeleteTarget(null)} />
      )}
    </div>
  )
}
