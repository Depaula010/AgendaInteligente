import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Edit2, Trash2, UserPlus, Users } from 'lucide-react'
import { Button } from '@/shared/components/ui/Button'
import { Badge } from '@/shared/components/ui/Badge'
import { Modal } from '@/shared/components/ui/Modal'
import { appToast } from '@/shared/lib/toast'
import { useAuthStore } from '@/features/auth/store/authStore'
import { equipeService } from '@/features/equipe/services/equipe.service'
import { ProfessionalFormModal } from '@/features/equipe/components/ProfessionalFormModal'
import type { ProfessionalResponse } from '@/features/equipe/types/equipe.types'

// ── DeleteConfirmModal ─────────────────────────────────────────────────────────

function DeleteConfirmModal({
  professional,
  onClose,
}: {
  professional: ProfessionalResponse
  onClose: () => void
}) {
  const queryClient = useQueryClient()

  const mutation = useMutation({
    mutationFn: () => equipeService.remove(professional.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['professionals'] })
      queryClient.invalidateQueries({ queryKey: ['professionals-all'] })
      appToast.success('Profissional removido.')
      onClose()
    },
    onError: (err: unknown) =>
      appToast.error(appToast.apiError(err, 'Erro ao remover profissional.')),
  })

  return (
    <Modal isOpen onClose={onClose} title="Remover profissional" size="sm">
      <p className="text-sm text-slate-300 mb-6">
        Deseja remover <span className="font-semibold text-white">{professional.name}</span>? Esta
        ação não pode ser desfeita.
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

// ── ProfessionalCard ───────────────────────────────────────────────────────────

function ProfessionalCard({
  professional,
  isOwner,
  isSelf,
  onEdit,
  onDelete,
}: {
  professional: ProfessionalResponse
  isOwner: boolean
  isSelf: boolean
  onEdit: () => void
  onDelete: () => void
}) {
  const initial = professional.name[0]?.toUpperCase() ?? '?'
  const color = professional.calendarColor ?? '#0ea5e9'

  return (
    <div className="flex items-center gap-4 rounded-xl border border-white/10 bg-white/5 px-4 py-3.5">
      {/* Avatar */}
      <div
        className="h-10 w-10 rounded-xl flex items-center justify-center flex-shrink-0"
        style={{ backgroundColor: `${color}33` }}
        aria-hidden="true"
      >
        <span className="text-sm font-bold" style={{ color }}>
          {initial}
        </span>
      </div>

      {/* Info */}
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          <p className="text-sm font-medium text-white truncate">{professional.name}</p>
          {isSelf && (
            <Badge variant="outline" className="text-[10px] px-1.5 py-0">
              Você
            </Badge>
          )}
          <Badge variant={professional.isActive ? 'success' : 'default'}>
            {professional.isActive ? 'Ativo' : 'Inativo'}
          </Badge>
        </div>
        <p className="text-xs text-slate-400 mt-0.5 truncate">{professional.email}</p>
        <p className="text-xs text-slate-500 mt-0.5">
          {professional.role === 'Owner'
            ? 'Proprietário'
            : professional.role === 'Receptionist'
              ? 'Recepcionista'
              : 'Colaborador'}
        </p>
      </div>

      {/* Color swatch */}
      <div
        className="h-4 w-4 rounded-full flex-shrink-0"
        style={{ backgroundColor: color }}
        title="Cor no calendário"
        aria-hidden="true"
      />

      {/* Owner actions — cannot delete self */}
      {isOwner && (
        <div className="flex items-center gap-1 flex-shrink-0">
          <button
            type="button"
            onClick={onEdit}
            className="p-2 rounded-lg text-slate-400 hover:text-white hover:bg-white/10 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
            aria-label={`Editar ${professional.name}`}
          >
            <Edit2 className="h-4 w-4" />
          </button>
          {!isSelf && (
            <button
              type="button"
              onClick={onDelete}
              className="p-2 rounded-lg text-slate-400 hover:text-red-400 hover:bg-red-500/10 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-500"
              aria-label={`Remover ${professional.name}`}
            >
              <Trash2 className="h-4 w-4" />
            </button>
          )}
        </div>
      )}
    </div>
  )
}

// ── EquipePage ─────────────────────────────────────────────────────────────────

export function EquipePage() {
  const user  = useAuthStore((s) => s.user)
  const isOwner = user?.role === 'Owner'

  const [formTarget, setFormTarget] = useState<ProfessionalResponse | null | 'new'>(null)
  const [deleteTarget, setDeleteTarget] = useState<ProfessionalResponse | null>(null)

  const { data: professionals = [], isLoading } = useQuery({
    queryKey: ['professionals-all'],
    queryFn: equipeService.getAll,
  })

  const activeCount = professionals.filter((p) => p.isActive).length

  return (
    <div className="p-4 md:p-6 max-w-3xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="font-display text-xl font-bold text-white">Equipe</h1>
          {professionals.length > 0 && (
            <p className="text-sm text-slate-500 mt-0.5">
              {activeCount} ativo{activeCount !== 1 ? 's' : ''} de {professionals.length}
            </p>
          )}
        </div>
        <div className="flex items-center gap-2">
          {isOwner && (
            <Button size="sm" onClick={() => setFormTarget('new')}>
              <UserPlus className="h-4 w-4 mr-1" />
              Novo profissional
            </Button>
          )}
          {!isOwner && (
            <div className="h-9 w-9 rounded-xl bg-brand-500/10 flex items-center justify-center">
              <Users className="h-5 w-5 text-brand-400" aria-hidden="true" />
            </div>
          )}
        </div>
      </div>

      {/* List */}
      {isLoading ? (
        <div className="flex flex-col gap-2">
          {[0, 1, 2, 3].map((i) => (
            <div key={i} className="h-[76px] rounded-xl bg-white/5 animate-pulse" />
          ))}
        </div>
      ) : professionals.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <Users className="h-10 w-10 text-slate-700 mb-3" aria-hidden="true" />
          <p className="text-white font-medium">Nenhum profissional cadastrado</p>
          {isOwner && (
            <p className="text-sm text-slate-500 mt-1">
              Clique em "Novo profissional" para adicionar o primeiro.
            </p>
          )}
        </div>
      ) : (
        <div className="flex flex-col gap-2">
          {professionals.map((professional) => (
            <ProfessionalCard
              key={professional.id}
              professional={professional}
              isOwner={isOwner}
              isSelf={professional.id === user?.id}
              onEdit={() => setFormTarget(professional)}
              onDelete={() => setDeleteTarget(professional)}
            />
          ))}
        </div>
      )}

      {/* Create / Edit modal */}
      {formTarget !== null && (
        <ProfessionalFormModal
          professional={formTarget === 'new' ? undefined : formTarget}
          onClose={() => setFormTarget(null)}
        />
      )}

      {/* Delete confirm */}
      {deleteTarget && (
        <DeleteConfirmModal
          professional={deleteTarget}
          onClose={() => setDeleteTarget(null)}
        />
      )}
    </div>
  )
}
