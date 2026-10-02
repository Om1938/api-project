import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, useSyncExternalStore, type ReactNode } from 'react'
import { api } from '../../api/client'
import type { AuthResponse, LoginRequest, RegisterRequest } from '../../api/types'
import { sessionStore } from './sessionStore'
import { AuthContext, type AuthContextValue } from './useAuth'

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const session = useSyncExternalStore(sessionStore.subscribe, sessionStore.get)
  const userId = session?.user.id

  // drop the previous user's cached data
  useEffect(() => () => queryClient.clear(), [queryClient, userId])

  const value = useMemo<AuthContextValue>(
    () => ({
      user: session?.user ?? null,
      login: async (request: LoginRequest) => sessionStore.set(await api.post<AuthResponse>('/api/auth/login', request)),
      register: async (request: RegisterRequest) => sessionStore.set(await api.post<AuthResponse>('/api/auth/register', request)),
      logout: () => sessionStore.set(null),
    }),
    [session],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
