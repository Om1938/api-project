import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { formatBucket, formatBucketLong } from '../../lib/format'
import { axisTick } from './chartTheme'
import { ChartCard, TooltipBox, TooltipValue } from './ChartCard'
import type { TrendPoint } from './series'

type Props = {
  title: string
  hint?: string
  points: TrendPoint[]
  bucketMinutes: number
  valueLabel: string
  format: (value: number) => string
  area?: boolean
  domain?: [number, number]
}

export function TrendChart({ title, hint, points, bucketMinutes, valueLabel, format, area = false, domain }: Props) {
  const measured = points.filter((point): point is { bucketStart: string; value: number } => point.value !== null)

  return (
    <ChartCard
      title={title}
      hint={hint}
      isEmpty={measured.every((point) => point.value === 0)}
      table={{
        rows: measured,
        rowKey: (point) => point.bucketStart,
        columns: [
          { header: bucketMinutes < 1440 ? 'Hour' : 'Day', cell: (point) => formatBucketLong(point.bucketStart, bucketMinutes) },
          { header: valueLabel, numeric: true, cell: (point) => format(point.value) },
        ],
      }}
    >
      <div className="h-52" role="img" aria-label={`Line chart: ${title}`}>
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={points} margin={{ top: 6, right: 6, bottom: 0, left: 0 }}>
            <CartesianGrid vertical={false} stroke="var(--line)" />
            <XAxis
              dataKey="bucketStart"
              tickFormatter={(value: string) => formatBucket(value, bucketMinutes)}
              tick={axisTick}
              tickLine={false}
              axisLine={{ stroke: 'var(--axis)' }}
              minTickGap={28}
            />
            <YAxis
              domain={domain ?? [0, 'auto']}
              tickFormatter={(value: number) => format(value)}
              tick={axisTick}
              tickLine={false}
              axisLine={false}
              width={56}
            />
            <Tooltip
              cursor={{ stroke: 'var(--axis)', strokeWidth: 1 }}
              isAnimationActive={false}
              content={({ active, payload }) => {
                const point = payload?.[0]?.payload as TrendPoint | undefined
                if (!active || !point || point.value === null) {
                  return null
                }
                return (
                  <TooltipBox title={formatBucketLong(point.bucketStart, bucketMinutes)}>
                    <TooltipValue value={format(point.value)} label={valueLabel} color="var(--series-1)" />
                  </TooltipBox>
                )
              }}
            />
            <Area
              dataKey="value"
              type="linear"
              stroke="var(--series-1)"
              strokeWidth={2}
              strokeLinejoin="round"
              strokeLinecap="round"
              fill="var(--series-1)"
              fillOpacity={area ? 0.1 : 0}
              dot={measured.length === 1 ? { r: 4, fill: 'var(--series-1)', stroke: 'var(--surface)', strokeWidth: 2 } : false}
              activeDot={{ r: 4, fill: 'var(--series-1)', stroke: 'var(--surface)', strokeWidth: 2 }}
              isAnimationActive={false}
            />
          </AreaChart>
        </ResponsiveContainer>
      </div>
    </ChartCard>
  )
}
