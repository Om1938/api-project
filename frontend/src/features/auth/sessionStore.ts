import type { AuthResponse } from '../../api/types'

const STORAGE_KEY = 'api-gateway.session'

export type Session = AuthResponse

function load(): Session | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) {
      return null
    }

    const session = JSON.parse(raw) as Session
    return new Date(session.expiresAt) > new Date() ? session : null
  } catch {
    return null
  }
}

let current = load()
const listeners = new Set<() => void>()

export const sessionStore = {
  get: () => current,

  set(next: Session | null) {
    current = next
    if (next) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(next))
    } else {
      localStorage.removeItem(STORAGE_KEY)
    }
    listeners.forEach((listener) => listener())
  },

  subscribe(listener: () => void) {
    listeners.add(listener)
    return () => {
      listeners.delete(listener)
    }
  },
}
