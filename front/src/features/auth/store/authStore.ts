import { create } from 'zustand'
import { persist, createJSONStorage } from 'zustand/middleware'
import { jwtDecode } from 'jwt-decode'
import type { AuthUser, JwtClaims } from '@/features/auth/types/auth.types'

interface AuthState {
  token: string | null
  user: AuthUser | null
  isAuthenticated: boolean
  setToken: (token: string) => void
  logout: () => void
}

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
            tenantId: claims.tenantId,
            role: claims.role,
          }
          set({ token, user, isAuthenticated: true })
        } catch {
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
      partialize: (state) => ({ token: state.token }),
      onRehydrateStorage: () => (state) => {
        if (state?.token) {
          state.setToken(state.token)
        }
      },
    },
  ),
)
