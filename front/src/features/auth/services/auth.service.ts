import { api } from '@/shared/lib/axios'
import type {
  AuthResponse,
  LoginRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
} from '@/features/auth/types/auth.types'

export const authService = {
  async login(data: LoginRequest): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/auth/login', data)
    return response.data
  },

  async refresh(refreshToken: string): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/auth/refresh', { refreshToken })
    return response.data
  },

  async forgotPassword(data: ForgotPasswordRequest): Promise<void> {
    await api.post('/auth/forgot-password', data)
  },

  async resetPassword(data: ResetPasswordRequest): Promise<void> {
    await api.post('/auth/reset-password', data)
  },
}
