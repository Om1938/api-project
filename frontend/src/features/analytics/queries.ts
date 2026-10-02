import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { api, query } from '../../api/client'
import type { ConsumerUsageDto, OwnerAnalyticsDto, UsageReportDto } from '../../api/types'
import { LIVE_REFRESH_MS } from '../../lib/queries'
import { rangeStart } from './range'
import { useAnalyticsFilters } from './useAnalyticsFilters'

export type Audience = 'owner' | 'consumer'

export type UsageData = { report: UsageReportDto; consumers: ConsumerUsageDto[] }

export function useUsageData(audience: Audience, apiId?: string) {
  const filters = useAnalyticsFilters()
  const scopedApi = (apiId ?? filters.apiId) || undefined
  const { range, bucket } = filters

  return useQuery({
    // keyed by preset, not timestamp: "from" is recomputed on every refetch
    queryKey: ['usage', audience, range, bucket ?? 'auto', scopedApi ?? 'all'],
    queryFn: async (): Promise<UsageData> => {
      const window = { from: rangeStart(range), bucketMinutes: bucket?.toString() }
      if (audience === 'owner') {
        return api.get<OwnerAnalyticsDto>(`/api/analytics${query({ ...window, apiId: scopedApi })}`)
      }
      return { report: await api.get<UsageReportDto>(`/api/me/usage${query(window)}`), consumers: [] }
    },
    refetchInterval: LIVE_REFRESH_MS,
    placeholderData: keepPreviousData,
  })
}
