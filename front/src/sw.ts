/// <reference lib="webworker" />
import { precacheAndRoute } from 'workbox-precaching'
import { registerRoute } from 'workbox-routing'
import { NetworkFirst, StaleWhileRevalidate } from 'workbox-strategies'
import { ExpirationPlugin } from 'workbox-expiration'
import { CacheableResponsePlugin } from 'workbox-cacheable-response'

declare const self: ServiceWorkerGlobalScope & {
  __WB_MANIFEST: { url: string; revision: string | null }[]
}

const apiUrl: string = (import.meta.env.VITE_API_URL as string | undefined) ?? 'http://localhost:5000'

// Precache dos assets estáticos (manifest injetado pelo vite-plugin-pwa)
precacheAndRoute(self.__WB_MANIFEST)

// Agenda: NetworkFirst com fallback para cache (garante dados offline)
registerRoute(
  ({ url }: { url: URL }) => url.href.startsWith(`${apiUrl}/api/v1/schedules`),
  new NetworkFirst({
    cacheName: 'schedules-cache',
    networkTimeoutSeconds: 5,
    plugins: [
      new ExpirationPlugin({ maxEntries: 20, maxAgeSeconds: 60 * 60 * 24 }),
      new CacheableResponsePlugin({ statuses: [0, 200] }),
    ],
  }),
)

// Demais endpoints de API: StaleWhileRevalidate com TTL de 24h
registerRoute(
  ({ url }: { url: URL }) => url.href.startsWith(`${apiUrl}/api/`),
  new StaleWhileRevalidate({
    cacheName: 'api-cache',
    plugins: [
      new ExpirationPlugin({ maxEntries: 100, maxAgeSeconds: 60 * 60 * 24 }),
      new CacheableResponsePlugin({ statuses: [0, 200] }),
    ],
  }),
)

// Google Fonts
registerRoute(
  ({ url }: { url: URL }) => url.origin === 'https://fonts.googleapis.com',
  new StaleWhileRevalidate({ cacheName: 'google-fonts-cache' }),
)

// ── Push notifications ─────────────────────────────────────────────────────────

self.addEventListener('push', (event: PushEvent) => {
  if (!event.data) return
  const data = event.data.json() as { title: string; body: string; url: string }
  event.waitUntil(
    self.registration.showNotification(data.title, {
      body: data.body,
      icon: '/pwa-192x192.png',
      badge: '/pwa-64x64.png',
      tag: 'agenda-notification',
      data: { url: data.url },
    }),
  )
})

self.addEventListener('notificationclick', (event: NotificationEvent) => {
  event.notification.close()
  const targetUrl: string =
    (event.notification.data as { url?: string } | null)?.url ?? '/dashboard/agenda'
  event.waitUntil(
    self.clients.matchAll({ type: 'window' }).then((clientList) => {
      const existing = clientList.find((c) => c.url.includes('/dashboard'))
      if (existing) return existing.focus()
      return self.clients.openWindow(targetUrl)
    }),
  )
})
