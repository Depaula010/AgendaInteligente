import { api } from '@/shared/lib/axios'
import type {
  ProfessionalResponse,
  CreateProfessionalRequest,
  UpdateProfessionalRequest,
} from '../types/equipe.types'

export const equipeService = {
  async getAll(): Promise<ProfessionalResponse[]> {
    const res = await api.get<ProfessionalResponse[]>('/professionals', { params: { all: true } })
    return res.data
  },

  async create(data: CreateProfessionalRequest): Promise<ProfessionalResponse> {
    const res = await api.post<ProfessionalResponse>('/professionals', data)
    return res.data
  },

  async update(id: string, data: UpdateProfessionalRequest): Promise<ProfessionalResponse> {
    const res = await api.put<ProfessionalResponse>(`/professionals/${id}`, data)
    return res.data
  },

  async remove(id: string): Promise<void> {
    await api.delete(`/professionals/${id}`)
  },
}
