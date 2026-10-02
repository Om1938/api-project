import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import type { UserRole } from './api/types'
import { AppLayout, type NavItem } from './components/AppLayout'
import { OwnerDashboardPage } from './features/analytics/OwnerDashboardPage'
import { ApiDetailPage } from './features/apis/ApiDetailPage'
import { ApisPage } from './features/apis/ApisPage'
import { AuthPage } from './features/auth/AuthPage'
import { AuthProvider } from './features/auth/AuthProvider'
import { homePath, useAuth } from './features/auth/useAuth'
import { ConsumerDashboardPage, CreditsPage, MyKeysPage } from './features/billing/ConsumerPages'
import { ConsumersPage } from './features/consumers/ConsumersPage'

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false, refetchOnWindowFocus: false } },
})

const ownerNavigation: NavItem[] = [
  { to: '/owner', label: 'Overview', end: true },
  { to: '/owner/apis', label: 'APIs' },
  { to: '/owner/consumers', label: 'Consumers' },
]

const consumerNavigation: NavItem[] = [
  { to: '/consumer', label: 'Overview', end: true },
  { to: '/consumer/keys', label: 'My API keys' },
  { to: '/consumer/credits', label: 'Credits' },
]

function RoleArea({ role, navigation }: { role: UserRole; navigation: NavItem[] }) {
  const { user } = useAuth()

  if (!user) {
    return <Navigate to="/login" replace />
  }

  return user.role === role ? <AppLayout navigation={navigation} /> : <Navigate to={homePath[user.role]} replace />
}

function Home() {
  const { user } = useAuth()
  return <Navigate to={user ? homePath[user.role] : '/login'} replace />
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<AuthPage key="login" mode="login" />} />
            <Route path="/register" element={<AuthPage key="register" mode="register" />} />

            <Route path="/owner" element={<RoleArea role="Owner" navigation={ownerNavigation} />}>
              <Route index element={<OwnerDashboardPage />} />
              <Route path="apis" element={<ApisPage />} />
              <Route path="apis/:apiId" element={<ApiDetailPage />} />
              <Route path="consumers" element={<ConsumersPage />} />
            </Route>

            <Route path="/consumer" element={<RoleArea role="Consumer" navigation={consumerNavigation} />}>
              <Route index element={<ConsumerDashboardPage />} />
              <Route path="keys" element={<MyKeysPage />} />
              <Route path="credits" element={<CreditsPage />} />
            </Route>

            <Route path="*" element={<Home />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  )
}
