const integer = new Intl.NumberFormat('en-US')
const compact = new Intl.NumberFormat('en-US', { notation: 'compact', maximumFractionDigits: 1 })
const credits = new Intl.NumberFormat('en-US', { maximumFractionDigits: 4 })
const dateTime = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' })
const hour = new Intl.DateTimeFormat(undefined, { hour: '2-digit', minute: '2-digit' })
const day = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' })

export const formatNumber = (value: number) => integer.format(value)

export const formatCompact = (value: number) => (Math.abs(value) < 10_000 ? integer.format(value) : compact.format(value))

export const formatCredits = (value: number) => credits.format(value)

export const formatPercent = (value: number) => `${value.toFixed(value >= 99.95 || value === 0 ? 0 : 1)}%`

export const formatMs = (value: number) => `${value > 0 && value < 10 ? value.toFixed(1) : Math.round(value)}\u00a0ms`

export const formatDateTime = (iso: string) => dateTime.format(new Date(iso))

export function formatBucket(iso: string, bucketMinutes: number) {
  const date = new Date(iso)
  return bucketMinutes < 1440 ? hour.format(date) : day.format(date)
}

export function formatBucketLong(iso: string, bucketMinutes: number) {
  const date = new Date(iso)
  return bucketMinutes < 1440 ? `${day.format(date)}, ${hour.format(date)}` : day.format(date)
}
