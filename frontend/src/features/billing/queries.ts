import { useQuery } from '@tanstack/react-query'
import { api } from '../../api/client'
import type { CreditAccountDto, TopUpRequest } from '../../api/types'
import { LIVE_REFRESH_MS, useApiMutation } from '../../lib/queries'

export const useCreditAccount = () =>
  useQuery({
    queryKey: ['me', 'credits'],
    queryFn: () => api.get<CreditAccountDto>('/api/me/credits'),
    refetchInterval: LIVE_REFRESH_MS,
  })

export const useTopUp = () =>
  useApiMutation((request: TopUpRequest) => api.post<CreditAccountDto>('/api/me/credits/top-up', request))
