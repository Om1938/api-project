import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { CountDto } from '../../api/types'
import { formatCompact, formatNumber } from '../../lib/format'
import { axisTick } from './chartTheme'
import { ChartCard, TooltipBox, TooltipValue } from './ChartCard'

export function LatencyHistogram({ buckets }: { buckets: CountDto[] }) {
  return (
    <ChartCard
      title="Latency distribution"
      hint="Forwarded requests by how long the upstream took to answer (ms)."
      isEmpty={buckets.every((bucket) => bucket.requests === 0)}
      table={{
        rows: buckets,
        rowKey: (bucket) => bucket.label,
        columns: [
          { header: 'Latency (ms)', cell: (bucket) => bucket.label },
          { header: 'Requests', numeric: true, cell: (bucket) => formatNumber(bucket.requests) },
        ],
      }}
    >
      <div className="h-52" role="img" aria-label="Histogram of upstream latency">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={buckets} margin={{ top: 6, right: 6, bottom: 0, left: 0 }} barCategoryGap={2}>
            <CartesianGrid vertical={false} stroke="var(--line)" />
            <XAxis dataKey="label" tick={axisTick} tickLine={false} axisLine={{ stroke: 'var(--axis)' }} interval={0} />
            <YAxis
              allowDecimals={false}
              tickFormatter={(value: number) => formatCompact(value)}
              tick={axisTick}
              tickLine={false}
              axisLine={false}
              width={44}
            />
            <Tooltip
              cursor={{ fill: 'var(--line)', opacity: 0.5 }}
              isAnimationActive={false}
              content={({ active, payload }) => {
                const bucket = payload?.[0]?.payload as CountDto | undefined
                if (!active || !bucket) {
                  return null
                }
                return (
                  <TooltipBox title={`${bucket.label} ms`}>
                    <TooltipValue value={formatNumber(bucket.requests)} label="requests" color="var(--series-1)" />
                  </TooltipBox>
                )
              }}
            />
            <Bar dataKey="requests" fill="var(--series-1)" radius={[4, 4, 0, 0]} isAnimationActive={false} />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </ChartCard>
  )
}
