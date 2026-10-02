import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../features/auth/useAuth'
import { Button } from './Button'

export type NavItem = { to: string; label: string; end?: boolean }

export function AppLayout({ navigation }: { navigation: NavItem[] }) {
  const { user, logout } = useAuth()

  return (
    <div className="flex min-h-screen flex-col md:flex-row">
      <aside className="flex shrink-0 flex-col gap-6 border-b border-line bg-surface p-4 md:w-60 md:border-r md:border-b-0">
        <div>
          <p className="text-sm font-semibold text-ink">API Gateway</p>
          <p className="text-xs text-ink-2">{user?.role} console</p>
        </div>

        <nav className="flex flex-row flex-wrap gap-1 md:flex-col">
          {navigation.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                `rounded-md px-3 py-2 text-sm ${isActive ? 'bg-accent-soft font-medium text-ink' : 'text-ink-2 hover:bg-plane hover:text-ink'}`
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="mt-auto flex flex-col gap-2 border-t border-line pt-4">
          <div className="min-w-0">
            <p className="truncate text-sm text-ink">{user?.name}</p>
            <p className="truncate text-xs text-ink-2">{user?.email}</p>
          </div>
          <Button size="sm" onClick={logout}>
            Sign out
          </Button>
        </div>
      </aside>

      <main className="min-w-0 flex-1 p-6 md:p-8">
        <div className="mx-auto max-w-6xl">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
