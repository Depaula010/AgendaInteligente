import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Modal } from '@/shared/components/ui/Modal'
import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { Badge } from '@/shared/components/ui/Badge'
import { appToast } from '@/shared/lib/toast'
import { equipeService } from '@/features/equipe/services/equipe.service'
import type { ProfessionalResponse } from '@/features/equipe/types/equipe.types'

// ── Color presets ──────────────────────────────────────────────────────────────

const PRESET_COLORS = [
  '#0ea5e9', '#22c55e', '#f59e0b', '#ef4444', '#8b5cf6',
  '#ec4899', '#f97316', '#14b8a6', '#6366f1', '#84cc16',
]

// ── Schemas ────────────────────────────────────────────────────────────────────

const createSchema = z.object({
  name:              z.string().min(1, 'Informe o nome'),
  email:             z.string().email('E-mail inválido'),
  password:          z.string().min(6, 'Mínimo 6 caracteres'),
  calendarColor:     z.string().optional(),
  role:              z.enum(['Staff', 'Receptionist']),
  canManageServices: z.boolean(),
})

const editSchema = z.object({
  name:              z.string().min(1, 'Informe o nome'),
  calendarColor:     z.string().optional(),
  isActive:          z.boolean(),
  role:              z.enum(['Staff', 'Receptionist']).optional(),
  canManageServices: z.boolean().optional(),
})

type CreateValues = z.infer<typeof createSchema>
type EditValues   = z.infer<typeof editSchema>

// ── Props ──────────────────────────────────────────────────────────────────────

interface ProfessionalFormModalProps {
  professional?: ProfessionalResponse
  onClose: () => void
}

// ── Component ──────────────────────────────────────────────────────────────────

export function ProfessionalFormModal({ professional, onClose }: ProfessionalFormModalProps) {
  const isEdit = !!professional
  const queryClient = useQueryClient()

  // ── Create form ──
  const createForm = useForm<CreateValues>({
    resolver: zodResolver(createSchema),
    defaultValues: {
      name:              '',
      email:             '',
      password:          '',
      calendarColor:     PRESET_COLORS[0],
      role:              'Staff',
      canManageServices: false,
    },
  })

  // ── Edit form ──
  const editForm = useForm<EditValues>({
    resolver: zodResolver(editSchema),
    defaultValues: {
      name:              professional?.name ?? '',
      calendarColor:     professional?.calendarColor ?? PRESET_COLORS[0],
      isActive:          professional?.isActive ?? true,
      role:              (professional?.role === 'Owner' ? undefined : professional?.role) ?? 'Staff',
      canManageServices: professional?.canManageServices ?? false,
    },
  })

  // Watch helpers — always call hooks before any conditional
  const createColor             = createForm.watch('calendarColor') ?? PRESET_COLORS[0]
  const createRole              = createForm.watch('role')
  const editColor               = editForm.watch('calendarColor') ?? PRESET_COLORS[0]
  const editIsActive            = editForm.watch('isActive')
  const editRole                = editForm.watch('role')

  const selectedColor = isEdit ? editColor : createColor

  const onMutationSuccess = (msg: string) => {
    queryClient.invalidateQueries({ queryKey: ['professionals'] })
    queryClient.invalidateQueries({ queryKey: ['professionals-all'] })
    appToast.success(msg)
    onClose()
  }
  const onMutationError = (err: unknown) =>
    appToast.error(appToast.apiError(err, 'Erro ao salvar profissional.'))

  const createMutation = useMutation({
    mutationFn: (values: CreateValues) => equipeService.create(values),
    onSuccess: () => onMutationSuccess('Profissional criado.'),
    onError: onMutationError,
  })

  const editMutation = useMutation({
    mutationFn: (values: EditValues) => equipeService.update(professional!.id, values),
    onSuccess: () => onMutationSuccess('Profissional atualizado.'),
    onError: onMutationError,
  })

  // ── Color picker (shared) ──
  function ColorPicker({ setValue }: { setValue: (color: string) => void }) {
    return (
      <div className="flex flex-col gap-2">
        <span className="text-sm font-medium text-slate-300">Cor no calendário</span>
        <div className="flex flex-wrap gap-2">
          {PRESET_COLORS.map((color) => (
            <button
              key={color}
              type="button"
              onClick={() => setValue(color)}
              className="h-7 w-7 rounded-full transition-transform hover:scale-110 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
              style={{
                backgroundColor: color,
                boxShadow:
                  selectedColor === color
                    ? `0 0 0 2px #0f172a, 0 0 0 4px ${color}`
                    : undefined,
              }}
              aria-label={`Cor ${color}`}
              aria-pressed={selectedColor === color}
            />
          ))}
        </div>
      </div>
    )
  }

  // ── Role + permissions section (shared) ──
  function RoleSection({
    currentRole,
    canManage,
    onRoleChange,
    onCanManageChange,
    isOwnerProfessional = false,
  }: {
    currentRole: 'Staff' | 'Receptionist' | undefined
    canManage: boolean | undefined
    onRoleChange: (role: 'Staff' | 'Receptionist') => void
    onCanManageChange: (v: boolean) => void
    isOwnerProfessional?: boolean
  }) {
    if (isOwnerProfessional) {
      return (
        <div className="flex items-center justify-between py-0.5">
          <span className="text-sm font-medium text-slate-300">Perfil</span>
          <Badge variant="warning">Proprietário</Badge>
        </div>
      )
    }

    return (
      <div className="flex flex-col gap-3">
        {/* Role select */}
        <div className="flex items-center justify-between py-0.5">
          <span className="text-sm font-medium text-slate-300">Perfil</span>
          <div className="flex gap-1.5">
            {(['Staff', 'Receptionist'] as const).map((r) => (
              <button
                key={r}
                type="button"
                onClick={() => onRoleChange(r)}
                className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-colors ${
                  currentRole === r
                    ? 'bg-brand-500/20 text-brand-400 border border-brand-500/40'
                    : 'bg-surface-700 text-slate-400 border border-white/10 hover:text-slate-300'
                }`}
              >
                {r === 'Staff' ? 'Barbeiro' : 'Recepcionista'}
              </button>
            ))}
          </div>
        </div>

        {/* canManageServices checkbox — only when Receptionist */}
        {currentRole === 'Receptionist' && (
          <button
            type="button"
            onClick={() => onCanManageChange(!canManage)}
            className="flex items-center gap-3 px-3 py-2.5 rounded-xl border border-white/10 bg-white/5 text-left hover:bg-white/8 transition-colors"
          >
            <div className={`h-4 w-4 rounded border flex items-center justify-center flex-shrink-0 transition-colors ${
              canManage
                ? 'bg-brand-500 border-brand-500'
                : 'bg-transparent border-slate-500'
            }`}>
              {canManage && (
                <svg className="h-3 w-3 text-white" viewBox="0 0 12 12" fill="none">
                  <path d="M2 6l3 3 5-5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
              )}
            </div>
            <div>
              <p className="text-sm text-slate-300 font-medium">Gerenciar catálogo de serviços</p>
              <p className="text-xs text-slate-500 mt-0.5">Criar, editar e excluir serviços</p>
            </div>
          </button>
        )}
      </div>
    )
  }

  // ── Edit form render ──
  if (isEdit) {
    const { register, handleSubmit, setValue, formState: { errors } } = editForm

    return (
      <Modal isOpen onClose={onClose} title="Editar profissional" size="md">
        <form
          onSubmit={handleSubmit((v) => editMutation.mutate(v))}
          className="flex flex-col gap-4"
          noValidate
        >
          {/* Email read-only */}
          <div className="flex flex-col gap-1.5">
            <span className="text-sm font-medium text-slate-300">E-mail</span>
            <div className="field-base flex items-center text-slate-500 select-none cursor-default">
              {professional.email}
            </div>
          </div>

          {/* Role section */}
          <RoleSection
            currentRole={editRole as 'Staff' | 'Receptionist' | undefined}
            canManage={editForm.watch('canManageServices')}
            onRoleChange={(r) => {
              setValue('role', r)
              if (r === 'Staff') setValue('canManageServices', false)
            }}
            onCanManageChange={(v) => setValue('canManageServices', v)}
            isOwnerProfessional={professional.role === 'Owner'}
          />

          {/* Name */}
          <Input
            {...register('name')}
            id="pro-name"
            label="Nome"
            placeholder="Nome completo"
            error={errors.name?.message}
          />

          {/* Color */}
          <ColorPicker setValue={(c) => setValue('calendarColor', c)} />

          {/* Status */}
          <div className="flex items-center justify-between py-1">
            <span className="text-sm font-medium text-slate-300">Status</span>
            <button
              type="button"
              onClick={() => setValue('isActive', !editIsActive)}
              className={`flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
                editIsActive
                  ? 'bg-emerald-500/15 text-emerald-400 border border-emerald-500/30'
                  : 'bg-surface-700 text-slate-400 border border-white/10'
              }`}
            >
              <span className={`h-2 w-2 rounded-full ${editIsActive ? 'bg-emerald-400' : 'bg-slate-500'}`} />
              {editIsActive ? 'Ativo' : 'Inativo'}
            </button>
          </div>

          {/* Actions */}
          <div className="flex gap-3 pt-1">
            <Button type="button" variant="ghost" onClick={onClose} className="flex-1">
              Cancelar
            </Button>
            <Button type="submit" isLoading={editMutation.isPending} className="flex-1">
              Salvar
            </Button>
          </div>
        </form>
      </Modal>
    )
  }

  // ── Create form render ──
  const { register, handleSubmit, setValue, formState: { errors } } = createForm

  return (
    <Modal isOpen onClose={onClose} title="Novo profissional" size="md">
      <form
        onSubmit={handleSubmit((v) => createMutation.mutate(v))}
        className="flex flex-col gap-4"
        noValidate
      >
        <Input
          {...register('name')}
          id="pro-name"
          label="Nome"
          placeholder="Nome completo"
          error={errors.name?.message}
        />

        <Input
          {...register('email')}
          id="pro-email"
          type="email"
          label="E-mail"
          placeholder="colaborador@email.com"
          error={errors.email?.message}
        />

        <Input
          {...register('password')}
          id="pro-password"
          type="password"
          label="Senha"
          placeholder="Mínimo 6 caracteres"
          error={errors.password?.message}
        />

        {/* Role section */}
        <RoleSection
          currentRole={createRole}
          canManage={createForm.watch('canManageServices')}
          onRoleChange={(r) => {
            setValue('role', r)
            if (r === 'Staff') setValue('canManageServices', false)
          }}
          onCanManageChange={(v) => setValue('canManageServices', v)}
        />

        <ColorPicker setValue={(c) => setValue('calendarColor', c)} />

        <div className="flex gap-3 pt-1">
          <Button type="button" variant="ghost" onClick={onClose} className="flex-1">
            Cancelar
          </Button>
          <Button type="submit" isLoading={createMutation.isPending} className="flex-1">
            Criar profissional
          </Button>
        </div>
      </form>
    </Modal>
  )
}
