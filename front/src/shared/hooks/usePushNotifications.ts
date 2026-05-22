import { useState, useEffect } from 'react'
import { pushService } from '@/features/notificacoes/services/push.service'
import { appToast } from '@/shared/lib/toast'

function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4)
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/')
  const raw = atob(base64)
  const output = new Uint8Array(raw.length)
  for (let i = 0; i < raw.length; i++) output[i] = raw.charCodeAt(i)
  return output
}

interface UsePushNotificationsResult {
  isSupported: boolean
  isSubscribed: boolean
  isLoading: boolean
  toggle: () => void
}

export function usePushNotifications(vapidPublicKey: string | undefined): UsePushNotificationsResult {
  const [isSubscribed, setIsSubscribed] = useState(false)
  const [isLoading, setIsLoading] = useState(false)

  const isSupported =
    typeof window !== 'undefined' &&
    'serviceWorker' in navigator &&
    'PushManager' in window

  // Verifica subscription existente ao montar
  useEffect(() => {
    if (!isSupported || !vapidPublicKey) return
    navigator.serviceWorker.ready
      .then((reg) => reg.pushManager.getSubscription())
      .then((sub) => setIsSubscribed(!!sub))
      .catch(() => {})
  }, [isSupported, vapidPublicKey])

  async function subscribe() {
    if (!vapidPublicKey) return
    setIsLoading(true)
    try {
      const permission = await Notification.requestPermission()
      if (permission !== 'granted') {
        appToast.error('Permissão de notificação negada.')
        return
      }
      const reg = await navigator.serviceWorker.ready
      const sub = await reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(vapidPublicKey),
      })
      const json = sub.toJSON()
      const keys = json.keys ?? {}
      await pushService.subscribe({
        endpoint: json.endpoint ?? sub.endpoint,
        p256dh: keys['p256dh'] ?? '',
        auth: keys['auth'] ?? '',
      })
      setIsSubscribed(true)
      appToast.success('Notificações ativadas.')
    } catch {
      appToast.error('Não foi possível ativar as notificações.')
    } finally {
      setIsLoading(false)
    }
  }

  async function unsubscribe() {
    setIsLoading(true)
    try {
      const reg = await navigator.serviceWorker.ready
      const sub = await reg.pushManager.getSubscription()
      if (sub) {
        await sub.unsubscribe()
        await pushService.unsubscribe(sub.endpoint)
      }
      setIsSubscribed(false)
      appToast.success('Notificações desativadas.')
    } catch {
      appToast.error('Erro ao desativar notificações.')
    } finally {
      setIsLoading(false)
    }
  }

  return {
    isSupported,
    isSubscribed,
    isLoading,
    toggle: isSubscribed ? unsubscribe : subscribe,
  }
}
