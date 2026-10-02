import { useQuery } from '@tanstack/react-query'
import { api } from '../../api/client'
import type { ApiDto, CreateApiRequest, UpdateApiRequest } from '../../api/types'
import { useApiMutation } from '../../lib/queries'

export const apiPath = (apiId: string, child = '') => `/api/apis/${apiId}${child}`

export const useApis = () => useQuery({ queryKey: ['apis'], queryFn: () => api.get<ApiDto[]>('/api/apis') })

export const useApi = (apiId: string) =>
  useQuery({ queryKey: ['apis', apiId], queryFn: () => api.get<ApiDto>(apiPath(apiId)) })

export const useCreateApi = () => useApiMutation((request: CreateApiRequest) => api.post<ApiDto>('/api/apis', request))

export const useUpdateApi = (apiId: string) =>
  useApiMutation((request: UpdateApiRequest) => api.put<ApiDto>(apiPath(apiId), request))

export const useDeleteApi = () => useApiMutation((apiId: string) => api.delete(apiPath(apiId)))
