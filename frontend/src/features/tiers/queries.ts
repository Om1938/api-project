import { useQuery } from '@tanstack/react-query'
import { api } from '../../api/client'
import type { TierDto, TierRequest } from '../../api/types'
import { useApiMutation } from '../../lib/queries'
import { apiPath } from '../apis/queries'

export const useTiers = (apiId: string) =>
  useQuery({ queryKey: ['apis', apiId, 'tiers'], queryFn: () => api.get<TierDto[]>(apiPath(apiId, '/tiers')) })

export const useSaveTier = (apiId: string, tierId?: string) =>
  useApiMutation((request: TierRequest) =>
    tierId
      ? api.put<TierDto>(apiPath(apiId, `/tiers/${tierId}`), request)
      : api.post<TierDto>(apiPath(apiId, '/tiers'), request),
  )

export const useDeleteTier = (apiId: string) =>
  useApiMutation((tierId: string) => api.delete(apiPath(apiId, `/tiers/${tierId}`)))
