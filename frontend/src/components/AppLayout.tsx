import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../features/auth/useAuth'
import { Button } from './Button'

export type NavItem = { to: string; label: string; end?: boolean }
export type NavGroup = { label: string; children: NavItem[] }
export type NavEntry = NavItem | NavGroup

const linkClass = ({ isActive }: { isActive: boolean }) =>
  `rounded-md px-3 py-2 text-sm ${isActive ? 'bg-accent-soft font-medium text-ink' : 'text-ink-2 hover:bg-plane hover:text-ink'}`

function NavEntryView({ entry }: { entry: NavEntry }) {
  if ('children' in entry) {
    return (
      <div className="flex flex-col gap-1">
        <p className="px-3 pt-2 text-xs font-medium tracking-wide text-muted uppercase">{entry.label}</p>
        <div className="ml-3 flex flex-row flex-wrap gap-1 border-l border-line pl-2 md:flex-col">
          {entry.children.map((child) => (
            <NavLink key={child.to} to={child.to} end={child.end} className={linkClass}>
              {child.label}
            </NavLink>
          ))}
        </div>
      </div>
    )
  }

  return (
    <NavLink to={entry.to} end={entry.end} className={linkClass}>
      {entry.label}
    </NavLink>
  )
}

export function AppLayout({ navigation }: { navigation: NavEntry[] }) {
  const { user, logout } = useAuth()

  return (
    <div className="flex min-h-screen flex-col md:flex-row">
      <aside className="flex shrink-0 flex-col gap-6 border-b border-line bg-surface p-4 md:w-60 md:border-r md:border-b-0">
        <div>
          <p className="text-sm font-semibold text-ink">API Gateway</p>
          <p className="text-xs text-ink-2">{user?.role} console</p>
        </div>

        <nav className="flex flex-row flex-wrap gap-1 md:flex-col">
          {navigation.map((entry) => (
            <NavEntryView key={entry.label} entry={entry} />
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
