import { useEffect, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ChevronRight, Loader2, Search, Users } from 'lucide-react'
import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { Badge } from '@/shared/components/ui/Badge'
import { clientesService } from '@/features/clientes/services/clientes.service'
import { CustomerDetailModal } from '@/features/clientes/components/CustomerDetailModal'
import type { CustomerResponse } from '@/features/agenda/types/agenda.types'

// ── CustomerCard ───────────────────────────────────────────────────────────────

function CustomerCard({
  customer,
  onClick,
}: {
  customer: CustomerResponse
  onClick: () => void
}) {
  const initial = customer.name[0]?.toUpperCase() ?? '?'

  const lastVisitLabel = customer.lastVisitAt
    ? new Date(customer.lastVisitAt).toLocaleDateString('pt-BR', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
      })
    : null

  return (
    <button
      type="button"
      onClick={onClick}
      className="w-full flex items-center gap-4 rounded-xl border border-white/10 bg-white/5 px-4 py-3.5 text-left transition-colors hover:bg-white/8 hover:border-white/15 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
    >
      {/* Avatar */}
      <div className="h-10 w-10 rounded-xl bg-brand-500/20 flex items-center justify-center flex-shrink-0">
        <span className="text-sm font-bold text-brand-400">{initial}</span>
      </div>

      {/* Info */}
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium text-white truncate">{customer.name}</p>
        <p className="text-xs text-slate-400 mt-0.5 truncate">{customer.phoneNumber}</p>
        {lastVisitLabel ? (
          <p className="text-xs text-slate-500 mt-0.5">Última visita: {lastVisitLabel}</p>
        ) : (
          <Badge variant="outline" className="mt-1 text-[10px] px-1.5 py-0">
            Novo
          </Badge>
        )}
      </div>

      <ChevronRight className="h-4 w-4 text-slate-600 flex-shrink-0" aria-hidden="true" />
    </button>
  )
}

// ── ClientesPage ───────────────────────────────────────────────────────────────

export function ClientesPage() {
  const [searchInput, setSearchInput] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [page, setPage] = useState(1)
  const [selectedCustomer, setSelectedCustomer] = useState<CustomerResponse | null>(null)
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Debounce search input 300ms
  useEffect(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current)
    debounceRef.current = setTimeout(() => {
      setDebouncedSearch(searchInput)
      setPage(1)
    }, 300)
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current)
    }
  }, [searchInput])

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ['customers', debouncedSearch, page],
    queryFn: () => clientesService.searchCustomers(debouncedSearch, page),
    placeholderData: (prev) => prev,
  })

  const customers = data?.items ?? []
  const total = data?.total ?? 0
  const pageSize = data?.pageSize ?? 20
  const totalPages = Math.max(1, Math.ceil(total / pageSize))

  return (
    <div className="p-4 md:p-6 max-w-3xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="font-display text-xl font-bold text-white">Clientes</h1>
          {total > 0 && (
            <p className="text-sm text-slate-500 mt-0.5">{total} cliente{total !== 1 ? 's' : ''}</p>
          )}
        </div>
        <div className="h-9 w-9 rounded-xl bg-brand-500/10 flex items-center justify-center">
          <Users className="h-5 w-5 text-brand-400" aria-hidden="true" />
        </div>
      </div>

      {/* Search */}
      <div className="relative mb-4">
        <Input
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          placeholder="Buscar por nome ou telefone..."
          leftIcon={<Search className="h-4 w-4" />}
          aria-label="Buscar clientes"
        />
        {isFetching && !isLoading && (
          <Loader2 className="absolute right-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-500 animate-spin" />
        )}
      </div>

      {/* List */}
      {isLoading ? (
        <div className="flex flex-col gap-2">
          {[0, 1, 2, 3].map((i) => (
            <div key={i} className="h-[72px] rounded-xl bg-white/5 animate-pulse" />
          ))}
        </div>
      ) : customers.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <Users className="h-10 w-10 text-slate-700 mb-3" aria-hidden="true" />
          <p className="text-white font-medium">Nenhum cliente encontrado</p>
          <p className="text-sm text-slate-500 mt-1">
            {debouncedSearch
              ? 'Tente outro nome ou telefone.'
              : 'Os clientes aparecem aqui após o primeiro agendamento.'}
          </p>
        </div>
      ) : (
        <div className="flex flex-col gap-2">
          {customers.map((customer) => (
            <CustomerCard
              key={customer.id}
              customer={customer}
              onClick={() => setSelectedCustomer(customer)}
            />
          ))}
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between mt-6">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page <= 1}
          >
            Anterior
          </Button>
          <span className="text-sm text-slate-400">
            {page} / {totalPages}
          </span>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            disabled={page >= totalPages}
          >
            Próximo
          </Button>
        </div>
      )}

      {/* Detail modal */}
      {selectedCustomer && (
        <CustomerDetailModal
          customer={selectedCustomer}
          onClose={() => setSelectedCustomer(null)}
        />
      )}
    </div>
  )
}
