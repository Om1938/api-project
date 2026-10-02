import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import type { UserRole } from './api/types'
import { AppLayout, type NavEntry } from './components/AppLayout'
import { AnalyticsFiltersProvider } from './features/analytics/AnalyticsFilters'
import { AnalyticsPage } from './features/analytics/AnalyticsPage'
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

const ownerNavigation: NavEntry[] = [
  { to: '/owner', label: 'Overview', end: true },
  {
    label: 'Analytics',
    children: [
      { to: '/owner/analytics/traffic', label: 'Traffic' },
      { to: '/owner/analytics/performance', label: 'Performance' },
      { to: '/owner/analytics/consumers', label: 'By consumer' },
      { to: '/owner/analytics/credits', label: 'Credits' },
    ],
  },
  { to: '/owner/apis', label: 'APIs' },
  { to: '/owner/consumers', label: 'Consumers' },
]

const consumerNavigation: NavEntry[] = [
  { to: '/consumer', label: 'Overview', end: true },
  {
    label: 'Analytics',
    children: [
      { to: '/consumer/analytics/traffic', label: 'Traffic' },
      { to: '/consumer/analytics/performance', label: 'Performance' },
      { to: '/consumer/analytics/spending', label: 'Spending' },
    ],
  },
  { to: '/consumer/keys', label: 'My API keys' },
  { to: '/consumer/credits', label: 'Credits' },
]

function RoleArea({ role, navigation }: { role: UserRole; navigation: NavEntry[] }) {
  const { user } = useAuth()

  if (!user) {
    return <Navigate to="/login" replace />
  }

  if (user.role !== role) {
    return <Navigate to={homePath[user.role]} replace />
  }

  return (
    <AnalyticsFiltersProvider>
      <AppLayout navigation={navigation} />
    </AnalyticsFiltersProvider>
  )
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
              <Route index element={<AnalyticsPage audience="owner" view="overview" />} />
              <Route path="analytics/traffic" element={<AnalyticsPage audience="owner" view="traffic" />} />
              <Route path="analytics/performance" element={<AnalyticsPage audience="owner" view="performance" />} />
              <Route path="analytics/consumers" element={<AnalyticsPage audience="owner" view="consumers" />} />
              <Route path="analytics/credits" element={<AnalyticsPage audience="owner" view="credits" />} />
              <Route path="apis" element={<ApisPage />} />
              <Route path="apis/:apiId" element={<ApiDetailPage />} />
              <Route path="consumers" element={<ConsumersPage />} />
            </Route>

            <Route path="/consumer" element={<RoleArea role="Consumer" navigation={consumerNavigation} />}>
              <Route index element={<ConsumerDashboardPage />} />
              <Route path="analytics/traffic" element={<AnalyticsPage audience="consumer" view="traffic" />} />
              <Route path="analytics/performance" element={<AnalyticsPage audience="consumer" view="performance" />} />
              <Route path="analytics/spending" element={<AnalyticsPage audience="consumer" view="credits" />} />
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
