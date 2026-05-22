import { Link, Outlet, useLocation } from 'react-router-dom'
import {
  CalendarDays,
  Users,
  Scissors,
  Settings,
  MessageCircle,
  UserCog,
  LogOut,
} from 'lucide-react'
import { cn } from '@/shared/utils/cn'
import { ROUTES } from '@/app/routes'
import { useAuthStore } from '@/features/auth/store/authStore'

const NAV_ITEMS = [
  { label: 'Agenda', icon: CalendarDays, to: ROUTES.AGENDA },
  { label: 'Clientes', icon: Users, to: ROUTES.CLIENTES },
  { label: 'Equipe', icon: UserCog, to: ROUTES.EQUIPE },
  { label: 'Serviços', icon: Scissors, to: ROUTES.SERVICOS },
  { label: 'Configurações', icon: Settings, to: ROUTES.CONFIGURACOES },
  { label: 'WhatsApp', icon: MessageCircle, to: ROUTES.WHATSAPP },
] as const

const BOTTOM_NAV = [
  { label: 'Agenda', icon: CalendarDays, to: ROUTES.AGENDA },
  { label: 'Clientes', icon: Users, to: ROUTES.CLIENTES },
  { label: 'Serviços', icon: Scissors, to: ROUTES.SERVICOS },
  { label: 'WhatsApp', icon: MessageCircle, to: ROUTES.WHATSAPP },
  { label: 'Config', icon: Settings, to: ROUTES.CONFIGURACOES },
] as const

interface NavItemProps {
  label: string
  icon: React.ElementType
  to: string
}

function SidebarNavItem({ label, icon: Icon, to }: NavItemProps) {
  const { pathname } = useLocation()
  const isActive = pathname === to || pathname.startsWith(to + '/')
  return (
    <Link
      to={to}
      className={cn(
        'flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-colors',
        isActive
          ? 'bg-brand-500/15 text-brand-400'
          : 'text-slate-400 hover:text-white hover:bg-white/5',
      )}
    >
      <Icon className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
      {label}
    </Link>
  )
}

export function DashboardLayout() {
  const user = useAuthStore((s) => s.user)
  const logout = useAuthStore((s) => s.logout)
  const { pathname } = useLocation()

  const currentPage = NAV_ITEMS.find(
    (item) => pathname === item.to || pathname.startsWith(item.to + '/'),
  )

  const userInitial = user?.name?.[0]?.toUpperCase() ?? 'U'

  return (
    <div className="flex h-screen overflow-hidden bg-surface-900">
      {/* ── Desktop Sidebar ── */}
      <aside className="hidden md:flex flex-col w-60 flex-shrink-0 bg-surface-800 border-r border-white/5">
        {/* Brand */}
        <div className="flex items-center gap-3 px-4 py-5 border-b border-white/5">
          <div className="flex items-center justify-center h-8 w-8 rounded-lg bg-brand-500/20 border border-brand-500/30">
            <CalendarDays className="h-4 w-4 text-brand-400" aria-hidden="true" />
          </div>
          <span className="font-display text-white font-bold text-sm leading-tight">
            Agenda Inteligente
          </span>
        </div>

        {/* Nav */}
        <nav
          className="flex-1 p-3 flex flex-col gap-1 overflow-y-auto custom-scrollbar"
          aria-label="Navegação principal"
        >
          {NAV_ITEMS.map((item) => (
            <SidebarNavItem key={item.to} {...item} />
          ))}
        </nav>

        {/* User + Logout */}
        <div className="p-3 border-t border-white/5">
          <div className="flex items-center gap-3 px-3 py-2 mb-1">
            <div className="h-7 w-7 rounded-full bg-brand-500/20 flex items-center justify-center flex-shrink-0">
              <span className="text-xs font-semibold text-brand-400">{userInitial}</span>
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-xs font-medium text-white truncate">{user?.name ?? 'Usuário'}</p>
              <p className="text-xs text-slate-500 truncate">
                {user?.role === 'Owner' ? 'Proprietário' : 'Profissional'}
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={logout}
            className="flex items-center gap-3 px-3 py-2 w-full rounded-xl text-sm text-slate-400 hover:text-white hover:bg-white/5 transition-colors"
          >
            <LogOut className="h-4 w-4" aria-hidden="true" />
            Sair
          </button>
        </div>
      </aside>

      {/* ── Main ── */}
      <div className="flex-1 flex flex-col overflow-hidden min-w-0">
        {/* Mobile header */}
        <header className="md:hidden flex items-center justify-between h-14 px-4 bg-surface-800 border-b border-white/5 flex-shrink-0">
          <div className="flex items-center gap-2">
            <CalendarDays className="h-5 w-5 text-brand-400" aria-hidden="true" />
            <span className="font-display text-white font-bold text-sm">
              {currentPage?.label ?? 'Dashboard'}
            </span>
          </div>
          <div className="h-7 w-7 rounded-full bg-brand-500/20 flex items-center justify-center">
            <span className="text-xs font-semibold text-brand-400">{userInitial}</span>
          </div>
        </header>

        {/* Page content — padding-bottom on mobile for bottom nav */}
        <main className="flex-1 overflow-auto pb-16 md:pb-0">
          <Outlet />
        </main>

        {/* Mobile bottom nav */}
        <nav
          className="md:hidden fixed bottom-0 left-0 right-0 z-40 bg-surface-800 border-t border-white/5 flex h-16"
          aria-label="Navegação mobile"
          style={{ paddingBottom: 'env(safe-area-inset-bottom)' }}
        >
          {BOTTOM_NAV.map(({ label, icon: Icon, to }) => {
            const isActive = pathname === to || pathname.startsWith(to + '/')
            return (
              <Link
                key={to}
                to={to}
                className={cn(
                  'flex-1 flex flex-col items-center justify-center gap-0.5 text-xs transition-colors',
                  isActive ? 'text-brand-400' : 'text-slate-500 hover:text-slate-300',
                )}
              >
                <Icon className="h-5 w-5" aria-hidden="true" />
                <span>{label}</span>
              </Link>
            )
          })}
        </nav>
      </div>
    </div>
  )
}
