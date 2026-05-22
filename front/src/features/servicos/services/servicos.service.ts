import { api } from '@/shared/lib/axios'
import type { ServiceCatalogResponse } from '@/features/agenda/types/agenda.types'
import type { CreateServiceRequest, UpdateServiceRequest } from '../types/servicos.types'

export const servicosService = {
  async getAll(): Promise<ServiceCatalogResponse[]> {
    const res = await api.get<ServiceCatalogResponse[]>('/services', { params: { all: true } })
    return res.data
  },

  async create(data: CreateServiceRequest): Promise<ServiceCatalogResponse> {
    const res = await api.post<ServiceCatalogResponse>('/services', data)
    return res.data
  },

  async update(id: string, data: UpdateServiceRequest): Promise<ServiceCatalogResponse> {
    const res = await api.put<ServiceCatalogResponse>(`/services/${id}`, data)
    return res.data
  },

  async remove(id: string): Promise<void> {
    await api.delete(`/services/${id}`)
  },
}
