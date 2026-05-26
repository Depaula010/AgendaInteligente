export interface LoginRequest {
  email: string
  password: string
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ResetPasswordRequest {
  token: string
  newPassword: string
}

export interface RegisterRequest {
  businessName: string
  ownerName: string
  email: string
  password: string
  confirmPassword: string
}

export interface AuthResponse {
  token: string
  refreshToken?: string
  expiresIn: number
}

export interface JwtClaims {
  sub: string
  email: string
  name: string
  tenantId: string
  role: 'Owner' | 'Receptionist' | 'Staff'
  can_manage_services: string
  exp: number
  iat: number
}

export interface AuthUser {
  id: string
  email: string
  name: string
  tenantId: string
  role: 'Owner' | 'Receptionist' | 'Staff'
  canManageServices: boolean
}
