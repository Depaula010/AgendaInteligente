export interface ProfessionalResponse {
  id: string
  name: string
  email: string
  role: 'Owner' | 'Staff'
  calendarColor: string | null
  isActive: boolean
  createdAt: string
}

export interface CreateProfessionalRequest {
  name: string
  email: string
  password: string
  calendarColor?: string
}

export interface UpdateProfessionalRequest {
  name: string
  calendarColor?: string
  isActive: boolean
}
