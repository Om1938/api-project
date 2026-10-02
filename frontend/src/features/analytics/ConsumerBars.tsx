import type { ConsumerUsageDto } from '../../api/types'
import { formatNumber } from '../../lib/format'
import { ChartCard, SeriesLegend } from './ChartCard'
import { seriesValues, USAGE_SERIES } from './series'

const MAX_CONSUMERS = 8

export function ConsumerBars({ consumers }: { consumers: ConsumerUsageDto[] }) {
  const top = consumers.slice(0, MAX_CONSUMERS)
  const max = Math.max(1, ...top.map((consumer) => consumer.usage.totalRequests))

  return (
    <ChartCard title="Busiest consumers" hint="Requests per consumer, split by outcome." isEmpty={top.length === 0}>
      <div className="flex flex-col gap-3">
        <SeriesLegend />
        <ul className="flex flex-col gap-2.5">
          {top.map((consumer) => {
            const values = seriesValues(consumer.usage)
            return (
              <li key={consumer.consumerId} className="grid grid-cols-[minmax(0,11rem)_1fr_auto] items-center gap-3 text-sm">
                <span className="truncate text-ink" title={consumer.email}>
                  {consumer.name}
                </span>
                <span className="flex h-2.5 gap-0.5" style={{ width: `${(consumer.usage.totalRequests / max) * 100}%` }}>
                  {USAGE_SERIES.filter((series) => values[series.key] > 0).map((series) => (
                    <span
                      key={series.key}
                      title={`${series.label}: ${formatNumber(values[series.key])}`}
                      className="h-full min-w-0.5 last:rounded-r"
                      style={{ flexGrow: values[series.key], flexBasis: 0, background: series.color }}
                    />
                  ))}
                </span>
                <span className="text-ink-2 tabular-nums">{formatNumber(consumer.usage.totalRequests)}</span>
              </li>
            )
          })}
        </ul>
      </div>
    </ChartCard>
  )
}
