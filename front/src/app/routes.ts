export const ROUTES = {
  LOGIN: '/login',
  REGISTER: '/cadastro',
  FORGOT_PASSWORD: '/esqueci-senha',
  RESET_PASSWORD: '/redefinir-senha',
  DASHBOARD: '/dashboard',
  AGENDA: '/dashboard/agenda',
  CLIENTES: '/dashboard/clientes',
  EQUIPE: '/dashboard/equipe',
  SERVICOS: '/dashboard/servicos',
  CONFIGURACOES: '/dashboard/configuracoes',
  WHATSAPP: '/dashboard/whatsapp',
} as const

export type AppRoute = (typeof ROUTES)[keyof typeof ROUTES]
