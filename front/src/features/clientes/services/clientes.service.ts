import { api } from '@/shared/lib/axios'
import type { ScheduleResponse } from '@/features/agenda/types/agenda.types'
import type { CustomerPageResponse } from '../types/clientes.types'

const PAGE_SIZE = 20

export const clientesService = {
  async searchCustomers(search: string, page: number): Promise<CustomerPageResponse> {
    const res = await api.get<CustomerPageResponse>('/customers/list', {
      params: { search: search || undefined, page, pageSize: PAGE_SIZE },
    })
    return res.data
  },

  async getCustomerSchedules(customerId: string): Promise<ScheduleResponse[]> {
    const res = await api.get<ScheduleResponse[]>(`/customers/${customerId}/schedules`)
    return res.data
  },
}
