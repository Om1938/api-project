import { createContext, useContext } from 'react'
import type { LoginRequest, RegisterRequest, UserDto, UserRole } from '../../api/types'

export type AuthContextValue = {
  user: UserDto | null
  login: (request: LoginRequest) => Promise<void>
  register: (request: RegisterRequest) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth(): AuthContextValue {
  const value = useContext(AuthContext)
  if (!value) {
    throw new Error('useAuth must be used inside <AuthProvider>.')
  }
  return value
}

export const homePath: Record<UserRole, string> = {
  Owner: '/owner',
  Consumer: '/consumer',
}
