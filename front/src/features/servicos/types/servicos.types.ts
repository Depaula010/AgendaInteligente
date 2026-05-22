import type { ServiceCatalogResponse } from '@/features/agenda/types/agenda.types'

export type { ServiceCatalogResponse }

export interface CreateServiceRequest {
  name: string
  durationMinutes: number
  price: number
  description?: string
  calendarColor?: string
}

export interface UpdateServiceRequest {
  name: string
  durationMinutes: number
  price: number
  description?: string
  calendarColor?: string
  isActive: boolean
}
