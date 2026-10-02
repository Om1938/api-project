import type { CountDto, UsagePointDto, UsageSummaryDto } from '../../api/types'

export const USAGE_SERIES = [
  { key: 'successful', label: 'Successful', color: 'var(--series-1)' },
  { key: 'failed', label: 'Failed', color: 'var(--series-2)' },
  { key: 'rateLimited', label: 'Rate limited', color: 'var(--series-3)' },
  { key: 'otherRejected', label: 'Quota or credits', color: 'var(--series-4)' },
] as const

export type SeriesKey = (typeof USAGE_SERIES)[number]['key']

export type ChartRow = Record<SeriesKey, number> & {
  bucketStart: string
  total: number
  top: SeriesKey | null
}

export function seriesValues(usage: UsageSummaryDto): Record<SeriesKey, number> {
  return {
    successful: usage.successful,
    failed: usage.failed,
    rateLimited: usage.rateLimited,
    otherRejected: usage.quotaExceeded + usage.insufficientCredits,
  }
}

export function toChartRows(series: UsagePointDto[]): ChartRow[] {
  return series.map((point) => {
    const values = seriesValues(point.usage)
    const top = USAGE_SERIES.findLast((candidate) => values[candidate.key] > 0)?.key ?? null
    return { bucketStart: point.bucketStart, total: point.usage.totalRequests, top, ...values }
  })
}

export type TrendPoint = { bucketStart: string; value: number | null }

export function successRate(usage: UsageSummaryDto): number | null {
  const forwarded = usage.successful + usage.failed
  return forwarded === 0 ? null : (usage.successful / forwarded) * 100
}

export const successRateTrend = (series: UsagePointDto[]): TrendPoint[] =>
  series.map((point) => ({ bucketStart: point.bucketStart, value: successRate(point.usage) }))

export const latencyTrend = (series: UsagePointDto[]): TrendPoint[] =>
  series.map((point) => ({
    bucketStart: point.bucketStart,
    value: point.usage.successful + point.usage.failed === 0 ? null : point.averageLatencyMs,
  }))

export const creditsTrend = (series: UsagePointDto[]): TrendPoint[] =>
  series.map((point) => ({ bucketStart: point.bucketStart, value: point.usage.creditsConsumed }))

export type RankedItem = { label: string; value: number }

export const byRequests = (counts: CountDto[]): RankedItem[] =>
  counts.map((count) => ({ label: count.label, value: count.requests }))
