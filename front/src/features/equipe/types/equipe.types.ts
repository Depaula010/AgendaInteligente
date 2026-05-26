export interface ProfessionalResponse {
  id: string
  name: string
  email: string
  role: 'Owner' | 'Receptionist' | 'Staff'
  canManageServices: boolean
  calendarColor: string | null
  isActive: boolean
  createdAt: string
  workingHoursJson: string | null
}

export interface CreateProfessionalRequest {
  name: string
  email: string
  password: string
  calendarColor?: string
  role?: 'Receptionist' | 'Staff'
  canManageServices?: boolean
}

export interface UpdateProfessionalRequest {
  name: string
  calendarColor?: string
  isActive: boolean
  role?: 'Receptionist' | 'Staff'
  canManageServices?: boolean
}
