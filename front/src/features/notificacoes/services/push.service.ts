import { api } from '@/shared/lib/axios'

export const pushService = {
  getVapidPublicKey: async (): Promise<string> => {
    const res = await api.get<{ publicKey: string }>('/push/vapid-public-key')
    return res.data.publicKey
  },

  subscribe: async (data: { endpoint: string; p256dh: string; auth: string }): Promise<void> => {
    await api.post('/push/subscribe', data)
  },

  unsubscribe: async (endpoint: string): Promise<void> => {
    await api.delete('/push/subscribe', { data: { endpoint } })
  },
}
