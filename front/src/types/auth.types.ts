// ── Autenticação ──────────────────────────────────────────────────────────────

export interface LoginRequest {
  email: string
  password: string
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

// Claims extraídos do JWT após decode
export interface JwtClaims {
  sub: string       // ProfessionalId (Guid)
  email: string
  tenantId: string  // TenantId (Guid) — isolamento SaaS
  role: 'Owner' | 'Staff'
  name: string
  exp: number
  iat: number
}

// Estado do usuário autenticado (store)
export interface AuthUser {
  id: string
  email: string
  name: string
  tenantId: string
  role: 'Owner' | 'Staff'
}
