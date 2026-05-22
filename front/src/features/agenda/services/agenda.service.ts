import { api } from '@/shared/lib/axios'
import type {
  ProfessionalResponse,
  ScheduleResponse,
  ScheduleStatusValue,
  ServiceCatalogResponse,
} from '../types/agenda.types'

export const agendaService = {
  async getSchedules(from: string, to: string, professionalId?: string): Promise<ScheduleResponse[]> {
    const params: Record<string, string> = { from, to }
    if (professionalId) params.professionalId = professionalId
    const res = await api.get<ScheduleResponse[]>('/schedules', { params })
    return res.data
  },

  async getProfessionals(): Promise<ProfessionalResponse[]> {
    const res = await api.get<ProfessionalResponse[]>('/professionals')
    return res.data
  },

  async getServices(): Promise<ServiceCatalogResponse[]> {
    const res = await api.get<ServiceCatalogResponse[]>('/services')
    return res.data
  },

  async updateStatus(id: string, status: ScheduleStatusValue): Promise<void> {
    await api.patch(`/schedules/${id}/status`, { status })
  },

  async deleteSchedule(id: string): Promise<void> {
    await api.delete(`/schedules/${id}`)
  },
}
