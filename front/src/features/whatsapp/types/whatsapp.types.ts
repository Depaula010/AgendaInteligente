export type WhatsAppConnectionStatus =
  | 'not_configured'
  | 'connecting'
  | 'connected'
  | 'unknown'

export interface WhatsAppStatus {
  status: WhatsAppConnectionStatus
  isConnected: boolean
  qrCode: string | null
}
