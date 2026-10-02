import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { api, query } from '../../api/client'
import type { OwnerAnalyticsDto, UsageReportDto } from '../../api/types'
import { LIVE_REFRESH_MS } from '../../lib/queries'
import { rangeStart, type RangePreset } from './range'

// keyed by preset, not timestamp: "from" is recomputed on every refetch
const live = { refetchInterval: LIVE_REFRESH_MS, placeholderData: keepPreviousData }

export const useOwnerAnalytics = (range: RangePreset, apiId?: string) =>
  useQuery({
    queryKey: ['analytics', range, apiId ?? 'all'],
    queryFn: () => api.get<OwnerAnalyticsDto>(`/api/analytics${query({ apiId, from: rangeStart(range) })}`),
    ...live,
  })

export const useMyUsage = (range: RangePreset) =>
  useQuery({
    queryKey: ['me', 'usage', range],
    queryFn: () => api.get<UsageReportDto>(`/api/me/usage${query({ from: rangeStart(range) })}`),
    ...live,
  })
