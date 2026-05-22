import { api } from '@/shared/lib/axios'
import type { AuthResponse, LoginRequest, RegisterRequest } from '@/features/auth/types/auth.types'

export const authService = {
  async login(data: LoginRequest): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/auth/login', data)
    return response.data
  },

  async register(data: RegisterRequest): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/auth/register', data)
    return response.data
  },
}
