export interface WorkingHourEntry {
  dayOfWeek: number  // 0=Domingo … 6=Sábado
  openTime: string   // "HH:mm"
  closeTime: string  // "HH:mm"
}

export interface TenantSettingsResponse {
  id: string
  workingHoursJson: string
  daysOffJson: string
  reminderLeadTimeHours: number
  reengagementInactiveDays: number
  botDisplayName: string | null
  whatsAppPhoneNumber: string | null
  conflictMessageTemplate: string | null
  hasGeminiApiKey: boolean
  geminiModel: string
}

export interface SaveTenantSettingsRequest {
  workingHoursJson: string
  daysOffJson: string
  reminderLeadTimeHours: number
  reengagementInactiveDays: number
  botDisplayName?: string
  whatsAppPhoneNumber?: string
  conflictMessageTemplate?: string
  geminiApiKey?: string  // undefined = não alterar; "" = remover; valor = definir
  geminiModel?: string
}
