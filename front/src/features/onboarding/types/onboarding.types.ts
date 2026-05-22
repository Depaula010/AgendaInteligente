export interface OnboardRequest {
  tenantName: string
  slug: string
  ownerName: string
  ownerEmail: string
  ownerPassword: string
}

export interface OnboardResponse {
  tenantId: string
  professionalId: string
  slug: string
}

export interface WizardData {
  tenantName: string
  slug: string
  ownerName: string
  ownerEmail: string
  ownerPassword: string
}

export interface CreateServiceRequest {
  name: string
  durationMinutes: number
  price: number
}
