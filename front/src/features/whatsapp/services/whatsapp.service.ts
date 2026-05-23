import { api } from '@/shared/lib/axios'
import type { WhatsAppSessionStats, WhatsAppStatus } from '../types/whatsapp.types'

export const whatsappService = {
  async getStatus(): Promise<WhatsAppStatus> {
    const res = await api.get<WhatsAppStatus>('/whatsapp/session/status')
    return res.data
  },

  async connect(): Promise<void> {
    await api.post('/whatsapp/session')
  },

  async reconnect(): Promise<void> {
    await api.post('/whatsapp/session/reconnect')
  },

  async getStats(): Promise<WhatsAppSessionStats> {
    const res = await api.get<WhatsAppSessionStats>('/whatsapp/session/stats')
    return res.data
  },
}
