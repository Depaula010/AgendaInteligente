import { useQuery } from '@tanstack/react-query'
import { configuracoesService } from '@/features/configuracoes/services/configuracoes.service'

const FALLBACK_TZ = 'America/Sao_Paulo'

/**
 * Retorna o fuso horário configurado pelo tenant.
 * Lê do cache do React Query (staleTime: Infinity) — sem request extra se já carregado.
 * Use em componentes que precisam de timezone reativo (ex: FullCalendar).
 * Para formatação de datas em utilitários puros, use setAppTimezone/getAppTimezone de date.ts.
 */
export function useTenantTimezone(): string {
  const { data } = useQuery({
    queryKey: ['tenant-settings'],
    queryFn: configuracoesService.get,
    staleTime: Infinity,
  })
  return data?.timeZoneId ?? FALLBACK_TZ
}
