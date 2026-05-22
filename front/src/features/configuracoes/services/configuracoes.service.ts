import { api } from '@/shared/lib/axios'
import type { TenantSettingsResponse, SaveTenantSettingsRequest } from '../types/configuracoes.types'

export const configuracoesService = {
  async get(): Promise<TenantSettingsResponse | null> {
    try {
      const res = await api.get<TenantSettingsResponse>('/tenant-settings')
      return res.data
    } catch (err: unknown) {
      if ((err as { response?: { status?: number } }).response?.status === 404) return null
      throw err
    }
  },

  async save(data: SaveTenantSettingsRequest): Promise<TenantSettingsResponse> {
    const res = await api.put<TenantSettingsResponse>('/tenant-settings', data)
    return res.data
  },
}
