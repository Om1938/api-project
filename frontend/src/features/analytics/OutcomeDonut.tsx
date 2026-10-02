import { Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts'
import type { UsageSummaryDto } from '../../api/types'
import { formatCompact, formatNumber, formatPercent } from '../../lib/format'
import { ChartCard, TooltipBox, TooltipValue } from './ChartCard'
import { seriesValues, USAGE_SERIES } from './series'

type Slice = { key: string; label: string; value: number; fill: string }

export function OutcomeDonut({ summary }: { summary: UsageSummaryDto }) {
  const values = seriesValues(summary)
  const total = summary.totalRequests
  const slices: Slice[] = USAGE_SERIES.map((series) => ({
    key: series.key,
    label: series.label,
    value: values[series.key],
    fill: series.color,
  }))
  const share = (value: number) => formatPercent(total === 0 ? 0 : (value / total) * 100)

  return (
    <ChartCard title="How requests ended" hint="Share of all metered requests by outcome." isEmpty={total === 0}>
      <div className="flex flex-wrap items-center gap-6">
        <div className="relative size-44 shrink-0" role="img" aria-label="Donut chart of request outcomes">
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={slices.filter((slice) => slice.value > 0)}
                dataKey="value"
                nameKey="label"
                innerRadius="64%"
                outerRadius="100%"
                startAngle={90}
                endAngle={-270}
                stroke="var(--surface)"
                strokeWidth={2}
                isAnimationActive={false}
              />
              <Tooltip
                isAnimationActive={false}
                content={({ active, payload }) => {
                  const slice = payload?.[0]?.payload as Slice | undefined
                  if (!active || !slice) {
                    return null
                  }
                  return (
                    <TooltipBox title={slice.label}>
                      <TooltipValue value={formatNumber(slice.value)} label={`requests (${share(slice.value)})`} color={slice.fill} />
                    </TooltipBox>
                  )
                }}
              />
            </PieChart>
          </ResponsiveContainer>
          <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
            <span className="text-xl font-semibold text-ink">{formatCompact(total)}</span>
            <span className="text-xs text-ink-2">requests</span>
          </div>
        </div>

        <ul className="flex min-w-48 flex-1 flex-col gap-2 text-sm">
          {slices.map((slice) => (
            <li key={slice.key} className="flex items-center gap-2">
              <span aria-hidden className="size-2.5 rounded-xs" style={{ background: slice.fill }} />
              <span className="text-ink-2">{slice.label}</span>
              <span className="ml-auto font-medium text-ink tabular-nums">{formatNumber(slice.value)}</span>
              <span className="w-12 text-right text-ink-2 tabular-nums">{share(slice.value)}</span>
            </li>
          ))}
        </ul>
      </div>
    </ChartCard>
  )
}
