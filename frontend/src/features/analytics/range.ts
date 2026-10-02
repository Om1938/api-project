export const RANGE_PRESETS = [
  { value: '1h', label: 'Last hour', hours: 1 },
  { value: '6h', label: 'Last 6 hours', hours: 6 },
  { value: '24h', label: 'Last 24 hours', hours: 24 },
  { value: '7d', label: 'Last 7 days', hours: 24 * 7 },
  { value: '30d', label: 'Last 30 days', hours: 24 * 30 },
] as const

export type RangePreset = (typeof RANGE_PRESETS)[number]['value']

export const DEFAULT_RANGE: RangePreset = '24h'

export const BUCKETS = [
  { minutes: 1, label: '1 minute', unit: 'minute' },
  { minutes: 5, label: '5 minutes', unit: '5 minutes' },
  { minutes: 30, label: '30 minutes', unit: '30 minutes' },
  { minutes: 60, label: '1 hour', unit: 'hour' },
  { minutes: 1440, label: '1 day', unit: 'day' },
] as const

// same cap as the backend, which coarsens anything finer
const MAX_POINTS = 400

const hoursOf = (preset: RangePreset) => RANGE_PRESETS.find((candidate) => candidate.value === preset)!.hours

export function rangeStart(preset: RangePreset, now = new Date()): string {
  return new Date(now.getTime() - hoursOf(preset) * 3_600_000).toISOString()
}

export function bucketOptions(preset: RangePreset) {
  const minutes = hoursOf(preset) * 60
  return BUCKETS.filter((bucket) => minutes / bucket.minutes >= 2 && minutes / bucket.minutes <= MAX_POINTS)
}

export const bucketUnit = (bucketMinutes: number) =>
  BUCKETS.find((bucket) => bucket.minutes === bucketMinutes)?.unit ?? `${bucketMinutes} minutes`
