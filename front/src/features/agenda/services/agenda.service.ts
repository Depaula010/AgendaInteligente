import { api } from '@/shared/lib/axios'
import type {
  AvailableSlotsResponse,
  CreateCustomerRequest,
  CreateRecurringScheduleRequest,
  CreateScheduleRequest,
  CustomerResponse,
  ProfessionalResponse,
  ScheduleResponse,
  ScheduleStatusValue,
  ServiceCatalogResponse,
  UpdateScheduleRequest,
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

  async getCustomerById(id: string): Promise<CustomerResponse> {
    const res = await api.get<CustomerResponse>(`/customers/${id}`)
    return res.data
  },

  async getCustomerByPhone(phone: string): Promise<CustomerResponse | null> {
    try {
      const res = await api.get<CustomerResponse>('/customers', { params: { phone } })
      return res.data
    } catch {
      return null
    }
  },

  async createCustomer(data: CreateCustomerRequest): Promise<CustomerResponse> {
    const res = await api.post<CustomerResponse>('/customers', data)
    return res.data
  },

  async createSchedule(data: CreateScheduleRequest): Promise<ScheduleResponse> {
    const res = await api.post<ScheduleResponse>('/schedules', data)
    return res.data
  },

  async updateSchedule(id: string, data: UpdateScheduleRequest): Promise<ScheduleResponse> {
    const res = await api.put<ScheduleResponse>(`/schedules/${id}`, data)
    return res.data
  },

  async createRecurringSchedule(data: CreateRecurringScheduleRequest): Promise<ScheduleResponse[]> {
    const res = await api.post<ScheduleResponse[]>('/schedules/recurring', data)
    return res.data
  },

  async getAvailableSlots(
    professionalId: string,
    serviceId: string,
    date: string,
  ): Promise<AvailableSlotsResponse> {
    const res = await api.get<AvailableSlotsResponse>('/schedules/available', {
      params: { professionalId, serviceId, date },
    })
    return res.data
  },
}
