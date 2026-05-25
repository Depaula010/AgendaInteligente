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
  timeZoneId: string
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
  timeZoneId?: string
}

export const BRAZIL_TIMEZONES = [
  { value: 'America/Sao_Paulo',   label: 'Brasília (UTC-3)' },
  { value: 'America/Belem',       label: 'Belém (UTC-3)' },
  { value: 'America/Fortaleza',   label: 'Fortaleza (UTC-3)' },
  { value: 'America/Recife',      label: 'Recife (UTC-3)' },
  { value: 'America/Manaus',      label: 'Manaus (UTC-4)' },
  { value: 'America/Porto_Velho', label: 'Porto Velho (UTC-4)' },
  { value: 'America/Boa_Vista',   label: 'Boa Vista (UTC-4)' },
  { value: 'America/Rio_Branco',  label: 'Rio Branco (UTC-5)' },
  { value: 'America/Noronha',     label: 'Fernando de Noronha (UTC-2)' },
] as const
