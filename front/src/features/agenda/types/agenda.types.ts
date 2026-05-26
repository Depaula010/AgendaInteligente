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
  customerName: string | null
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

export interface CustomerResponse {
  id: string
  name: string
  phoneNumber: string
  email: string | null
  lastVisitAt: string | null
}

export interface CreateCustomerRequest {
  name: string
  phoneNumber: string
  email?: string
}

export interface CreateScheduleRequest {
  customerId: string
  professionalId: string
  serviceId: string
  startDateTime: string
  notes?: string
}

export interface UpdateScheduleRequest {
  startDateTime: string
  notes?: string
}

export interface AvailableSlotsResponse {
  professionalId: string
  serviceId: string
  date: string
  durationMinutes: number
  slots: string[]
}

export interface ConflictInfo {
  error: string
  suggestedAlternatives: string[]
}

export interface CreateRecurringScheduleRequest {
  customerId: string
  professionalId: string
  serviceId: string
  startDateTime: string
  repeatType: 'weekly' | 'monthly'
  repeatCount?: number
  notes?: string
}

export interface RecurringConflictInfo {
  error: string
  conflictingDates: string[]
}
