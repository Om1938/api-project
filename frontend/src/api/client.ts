import { sessionStore } from '../features/auth/sessionStore'

export class ApiError extends Error {
  readonly status: number
  readonly code: string

  constructor(status: number, code: string, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const token = sessionStore.get()?.accessToken
  const response = await fetch(path, {
    method,
    headers: {
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (response.ok) {
    return (response.status === 204 ? undefined : await response.json()) as T
  }

  // a 401 without a token is just a failed login
  if (response.status === 401 && token) {
    sessionStore.set(null)
  }

  const problem = await response.json().catch(() => null)
  throw new ApiError(
    response.status,
    problem?.code ?? 'unknown_error',
    problem?.detail ?? problem?.title ?? `Request failed (${response.status})`,
  )
}

export const api = {
  get: <T>(path: string) => request<T>('GET', path),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, body),
  put: <T>(path: string, body: unknown) => request<T>('PUT', path, body),
  delete: (path: string) => request<void>('DELETE', path),
}

export function query(params: Record<string, string | undefined>): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined) {
      search.set(key, value)
    }
  }
  const text = search.toString()
  return text ? `?${text}` : ''
}

export function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Something went wrong.'
}
