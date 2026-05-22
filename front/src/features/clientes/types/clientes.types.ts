import type { CustomerResponse, ScheduleResponse } from '@/features/agenda/types/agenda.types'

export type { CustomerResponse, ScheduleResponse }

export interface CustomerPageResponse {
  items: CustomerResponse[]
  total: number
  page: number
  pageSize: number
}
