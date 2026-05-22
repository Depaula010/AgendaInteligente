import { create } from 'zustand'
import { persist, createJSONStorage } from 'zustand/middleware'
import { jwtDecode } from 'jwt-decode'
import type { AuthUser, JwtClaims } from '@/features/auth/types/auth.types'

interface AuthState {
  token: string | null
  refreshToken: string | null
  user: AuthUser | null
  isAuthenticated: boolean
  _hasHydrated: boolean

  setTokens: (token: string, refreshToken?: string) => void
  logout: () => void
  setHasHydrated: (value: boolean) => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      refreshToken: null,
      user: null,
      isAuthenticated: false,
      _hasHydrated: false,

      setTokens: (token: string, refreshToken?: string) => {
        try {
          const claims = jwtDecode<JwtClaims>(token)
          const user: AuthUser = {
            id: claims.sub,
            email: claims.email,
            name: claims.name,
            tenantId: claims.tenantId,
            role: claims.role,
          }
          set({ token, refreshToken: refreshToken ?? null, user, isAuthenticated: true })
        } catch {
          set({ token: null, refreshToken: null, user: null, isAuthenticated: false })
        }
      },

      logout: () => {
        set({ token: null, refreshToken: null, user: null, isAuthenticated: false })
      },

      setHasHydrated: (value: boolean) => {
        set({ _hasHydrated: value })
      },
    }),
    {
      name: 'agenda-auth-storage',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({ token: state.token, refreshToken: state.refreshToken }),
      onRehydrateStorage: () => (state) => {
        if (state?.token) {
          state.setTokens(state.token, state.refreshToken ?? undefined)
        }
        state?.setHasHydrated(true)
      },
    },
  ),
)
