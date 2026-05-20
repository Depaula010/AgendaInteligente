import { create } from 'zustand'
import { persist, createJSONStorage } from 'zustand/middleware'
import { jwtDecode } from 'jwt-decode'
import type { AuthUser, JwtClaims } from '@/types/auth.types'

interface AuthState {
  token: string | null
  user: AuthUser | null
  isAuthenticated: boolean

  // Ações
  setToken: (token: string) => void
  logout: () => void
}

/**
 * useAuthStore — store de autenticação global (Zustand + persistência no localStorage).
 *
 * Segurança SaaS: o TenantId é SEMPRE extraído dos claims do JWT emitido
 * pelo backend, nunca enviado manualmente pelo frontend. Isso garante que
 * um usuário não consiga alterar o TenantId e acessar dados de outro tenant.
 */
export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      user: null,
      isAuthenticated: false,

      setToken: (token: string) => {
        try {
          const claims = jwtDecode<JwtClaims>(token)
          const user: AuthUser = {
            id: claims.sub,
            email: claims.email,
            name: claims.name,
            tenantId: claims.tenantId, // Derivado do JWT — não editável pelo client
            role: claims.role,
          }
          set({ token, user, isAuthenticated: true })
        } catch {
          // Token inválido ou malformado
          set({ token: null, user: null, isAuthenticated: false })
        }
      },

      logout: () => {
        set({ token: null, user: null, isAuthenticated: false })
      },
    }),
    {
      name: 'agenda-auth-storage',
      storage: createJSONStorage(() => localStorage),
      // Persiste apenas o token; o user é re-derivado via setToken
      partialize: (state) => ({ token: state.token }),
      // Ao reidratar, reprocessa o token para popular o user
      onRehydrateStorage: () => (state) => {
        if (state?.token) {
          state.setToken(state.token)
        }
      },
    },
  ),
)
