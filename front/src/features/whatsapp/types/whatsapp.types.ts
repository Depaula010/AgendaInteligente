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

export interface WhatsAppSessionStats {
  sessionId: string
  isActive: boolean
  messagesReceived: number
  messagesSent: number
  webhookErrors: number
  circuitBreakerTrips: number
  reconnectCount: number
  connectedAt: string | null
  uptimeSeconds: number | null
}
