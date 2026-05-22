export const ScheduleStatus = {
  Pending: 0,
  Confirmed: 1,
  Cancelled: 2,
  Completed: 3,
  NoShow: 4,
} as const

export type ScheduleStatusValue = (typeof ScheduleStatus)[keyof typeof ScheduleStatus]

export interface ScheduleResponse {
  id: string
  customerId: string
  professionalId: string
  serviceId: string
  startDateTime: string
  endDateTime: string
  status: ScheduleStatusValue
  notes: string | null
  createdAt: string
}

export interface ProfessionalResponse {
  id: string
  name: string
  email: string
  role: 0 | 1
  calendarColor: string | null
  isActive: boolean
  createdAt: string
}

export interface ServiceCatalogResponse {
  id: string
  name: string
  durationMinutes: number
  price: number
  description: string | null
  calendarColor: string | null
  isActive: boolean
  createdAt: string
}
