import { useCallback, useState } from 'react'

type Theme = 'dark' | 'light'

const STORAGE_KEY = 'agenda-theme'

function applyTheme(theme: Theme) {
  const root = document.documentElement
  root.classList.remove('dark', 'light')
  root.classList.add(theme)
  localStorage.setItem(STORAGE_KEY, theme)
  // Atualiza theme-color da PWA
  const meta = document.querySelector<HTMLMetaElement>('meta[name="theme-color"]')
  if (meta) meta.content = theme === 'dark' ? '#0f172a' : '#f1f5f9'
}

function getInitialTheme(): Theme {
  if (typeof window === 'undefined') return 'dark'
  // O inline script já aplicou a classe — só lê para sincronizar o estado React
  return (localStorage.getItem(STORAGE_KEY) as Theme | null) ?? 'dark'
}

export function useDarkMode() {
  const [isDark, setIsDark] = useState<boolean>(() => getInitialTheme() === 'dark')

  const toggle = useCallback(() => {
    setIsDark((prev) => {
      const next = !prev
      applyTheme(next ? 'dark' : 'light')
      return next
    })
  }, [])

  return { isDark, toggle }
}
