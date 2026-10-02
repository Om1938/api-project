import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { sessionStore, type Session } from '../features/auth/sessionStore'
import { api, ApiError, query } from './client'

const session: Session = {
  accessToken: 'token-123',
  expiresAt: '2999-01-01T00:00:00Z',
  user: { id: 'u1', name: 'Olive Owner', email: 'olive@example.com', role: 'Owner', creditBalance: 0 },
}

const respond = (status: number, body?: unknown) =>
  new Response(body === undefined ? null : JSON.stringify(body), { status })

describe('api client', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    fetchMock.mockReset()
    sessionStore.set(null)
  })

  it('sends the bearer token and JSON body, and returns the parsed response', async () => {
    sessionStore.set(session)
    fetchMock.mockResolvedValue(respond(201, { id: 'a1' }))

    const result = await api.post<{ id: string }>('/api/apis', { name: 'Weather' })

    expect(result).toEqual({ id: 'a1' })
    const [path, init] = fetchMock.mock.calls[0]
    expect(path).toBe('/api/apis')
    expect(init).toMatchObject({
      method: 'POST',
      body: '{"name":"Weather"}',
      headers: { Authorization: 'Bearer token-123', 'Content-Type': 'application/json' },
    })
  })

  it('returns undefined for 204 responses', async () => {
    fetchMock.mockResolvedValue(respond(204))

    await expect(api.delete('/api/apis/a1')).resolves.toBeUndefined()
  })

  it('turns a problem response into an ApiError with its code and detail', async () => {
    fetchMock.mockResolvedValue(respond(409, { title: 'Conflict', detail: 'The slug is already in use.', code: 'slug_taken' }))

    const error = await api.post('/api/apis', {}).catch((caught: unknown) => caught)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 409, code: 'slug_taken', message: 'The slug is already in use.' })
  })

  it('ends the session when the server rejects the token', async () => {
    sessionStore.set(session)
    fetchMock.mockResolvedValue(respond(401))

    await expect(api.get('/api/apis')).rejects.toMatchObject({ status: 401 })
    expect(sessionStore.get()).toBeNull()
    expect(localStorage.length).toBe(0)
  })

  it('keeps no session and does not throw twice on a failed login', async () => {
    fetchMock.mockResolvedValue(respond(401, { detail: 'Email or password is incorrect.', code: 'invalid_credentials' }))

    await expect(api.post('/api/auth/login', {})).rejects.toMatchObject({ code: 'invalid_credentials' })
  })
})

describe('query', () => {
  it('skips undefined values and encodes the rest', () => {
    expect(query({ apiId: undefined, from: '2026-03-15T10:30:00.000Z' })).toBe('?from=2026-03-15T10%3A30%3A00.000Z')
    expect(query({ apiId: undefined })).toBe('')
  })
})
