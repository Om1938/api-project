export const RANGE_PRESETS = [
  { value: '24h', label: 'Last 24 hours', hours: 24 },
  { value: '7d', label: 'Last 7 days', hours: 24 * 7 },
  { value: '30d', label: 'Last 30 days', hours: 24 * 30 },
] as const

export type RangePreset = (typeof RANGE_PRESETS)[number]['value']

export const DEFAULT_RANGE: RangePreset = '24h'

export function rangeStart(preset: RangePreset, now = new Date()): string {
  const { hours } = RANGE_PRESETS.find((candidate) => candidate.value === preset)!
  return new Date(now.getTime() - hours * 3_600_000).toISOString()
}
