import { useQuery } from '@tanstack/react-query'
import { api } from '../../api/client'
import type { ApiKeyDto, CreateApiKeyRequest, CreatedApiKeyDto, MyKeyDto } from '../../api/types'
import { LIVE_REFRESH_MS, useApiMutation } from '../../lib/queries'
import { apiPath } from '../apis/queries'

export const useKeys = (apiId: string) =>
  useQuery({ queryKey: ['apis', apiId, 'keys'], queryFn: () => api.get<ApiKeyDto[]>(apiPath(apiId, '/keys')) })

export const useIssueKey = (apiId: string) =>
  useApiMutation((request: CreateApiKeyRequest) => api.post<CreatedApiKeyDto>(apiPath(apiId, '/keys'), request))

export const useRevokeKey = (apiId: string) =>
  useApiMutation((keyId: string) => api.post<ApiKeyDto>(apiPath(apiId, `/keys/${keyId}/revoke`)))

export const useMyKeys = () =>
  useQuery({ queryKey: ['me', 'keys'], queryFn: () => api.get<MyKeyDto[]>('/api/me/keys'), refetchInterval: LIVE_REFRESH_MS })
