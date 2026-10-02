import { describe, expect, it } from 'vitest'
import type { UsagePointDto, UsageSummaryDto } from '../../api/types'
import { bucketOptions, rangeStart } from './range'
import { latencyTrend, successRate, successRateTrend, toChartRows } from './series'

const usage = (overrides: Partial<UsageSummaryDto>): UsageSummaryDto => ({
  totalRequests: 0,
  successful: 0,
  failed: 0,
  rateLimited: 0,
  quotaExceeded: 0,
  insufficientCredits: 0,
  creditsConsumed: 0,
  ...overrides,
})

const point = (overrides: Partial<UsageSummaryDto>): UsagePointDto => ({
  bucketStart: '2026-03-15T10:00:00Z',
  usage: usage(overrides),
  averageLatencyMs: 40,
})

describe('toChartRows', () => {
  it('folds quota and credit rejections into one series', () => {
    const [row] = toChartRows([point({ totalRequests: 5, quotaExceeded: 2, insufficientCredits: 3 })])

    expect(row.otherRejected).toBe(5)
    expect(row.total).toBe(5)
  })

  it('marks the uppermost non-empty series as the top of the stack', () => {
    const rows = toChartRows([
      point({ successful: 4 }),
      point({ successful: 4, failed: 1, rateLimited: 2 }),
      point({}),
    ])

    expect(rows.map((row) => row.top)).toEqual(['successful', 'rateLimited', null])
  })
})

describe('rangeStart', () => {
  const now = new Date('2026-03-15T10:30:00Z')

  it.each([
    ['1h', '2026-03-15T09:30:00.000Z'],
    ['24h', '2026-03-14T10:30:00.000Z'],
    ['7d', '2026-03-08T10:30:00.000Z'],
    ['30d', '2026-02-13T10:30:00.000Z'],
  ] as const)('%s starts at %s', (preset, expected) => {
    expect(rangeStart(preset, now)).toBe(expected)
  })
})

describe('success rate', () => {
  it('is the share of forwarded requests that succeeded, ignoring gateway rejections', () => {
    expect(successRate(usage({ successful: 3, failed: 1, rateLimited: 50 }))).toBe(75)
  })

  it('is undefined, not zero, when nothing was forwarded', () => {
    expect(successRate(usage({ rateLimited: 5 }))).toBeNull()
    expect(successRateTrend([point({}), point({ successful: 2 })]).map((p) => p.value)).toEqual([null, 100])
  })
})

describe('latencyTrend', () => {
  it('leaves a gap for periods without forwarded requests', () => {
    expect(latencyTrend([point({ rateLimited: 4 }), point({ failed: 1 })]).map((p) => p.value)).toEqual([null, 40])
  })
})

describe('bucketOptions', () => {
  it.each([
    ['1h', [1, 5, 30]],
    ['6h', [1, 5, 30, 60]],
    ['24h', [5, 30, 60]],
    ['7d', [30, 60, 1440]],
    ['30d', [1440]],
  ] as const)('%s offers %j minutes', (preset, expected) => {
    expect(bucketOptions(preset).map((option) => option.minutes)).toEqual(expected)
  })
})
