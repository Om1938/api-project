import { Bar, BarChart, CartesianGrid, Rectangle, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { UsageReportDto } from '../../api/types'
import { formatBucket, formatBucketLong, formatCompact, formatNumber } from '../../lib/format'
import { axisTick } from './chartTheme'
import { ChartCard, SeriesLegend, TooltipBox, TooltipValue } from './ChartCard'
import { bucketUnit } from './range'
import { toChartRows, USAGE_SERIES, type ChartRow, type SeriesKey } from './series'

const SEGMENT_GAP = 2
const TOP_RADIUS = 4

type SegmentProps = { x?: number; y?: number; width?: number; height?: number; fill?: string; payload?: ChartRow }

function segmentShape(series: SeriesKey) {
  return function Segment({ x = 0, y = 0, width = 0, height = 0, fill, payload }: SegmentProps) {
    if (height <= 0) {
      return null
    }

    const isTop = payload?.top === series
    const gap = isTop ? 0 : Math.min(SEGMENT_GAP, height - 1)
    return (
      <Rectangle
        x={x}
        y={y + gap}
        width={width}
        height={height - gap}
        fill={fill}
        radius={isTop ? [TOP_RADIUS, TOP_RADIUS, 0, 0] : 0}
      />
    )
  }
}

export function UsageChart({ report }: { report: UsageReportDto }) {
  const rows = toChartRows(report.series)
  const bucketLabel = bucketUnit(report.bucketMinutes)

  return (
    <ChartCard
      title={`Requests per ${bucketLabel}`}
      hint="Every metered request, stacked by how it ended."
      isEmpty={report.summary.totalRequests === 0}
      table={{
        rows: rows.filter((row) => row.total > 0),
        rowKey: (row) => row.bucketStart,
        columns: [
          { header: report.bucketMinutes < 1440 ? 'Time' : 'Day', cell: (row) => formatBucketLong(row.bucketStart, report.bucketMinutes) },
          ...USAGE_SERIES.map((series) => ({
            header: series.label,
            numeric: true,
            cell: (row: ChartRow) => formatNumber(row[series.key]),
          })),
          { header: 'Total', numeric: true, cell: (row) => formatNumber(row.total) },
        ],
      }}
    >
      <div className="flex flex-col gap-3">
        <SeriesLegend />
        <div className="h-64" role="img" aria-label={`Stacked bar chart of requests per ${bucketLabel} by outcome`}>
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={rows} margin={{ top: 4, right: 4, bottom: 0, left: 0 }}>
              <CartesianGrid vertical={false} stroke="var(--line)" />
              <XAxis
                dataKey="bucketStart"
                tickFormatter={(value: string) => formatBucket(value, report.bucketMinutes)}
                tick={axisTick}
                tickLine={false}
                axisLine={{ stroke: 'var(--axis)' }}
                minTickGap={28}
              />
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
                  const row = payload?.[0]?.payload as ChartRow | undefined
                  if (!active || !row) {
                    return null
                  }
                  return (
                    <TooltipBox title={formatBucketLong(row.bucketStart, report.bucketMinutes)}>
                      <div className="flex flex-col gap-1">
                        {USAGE_SERIES.map((series) => (
                          <TooltipValue key={series.key} value={formatNumber(row[series.key])} label={series.label} color={series.color} />
                        ))}
                      </div>
                      <div className="mt-1.5 border-t border-line pt-1.5">
                        <TooltipValue value={formatNumber(row.total)} label="total" />
                      </div>
                    </TooltipBox>
                  )
                }}
              />
              {USAGE_SERIES.map((series) => (
                <Bar
                  key={series.key}
                  dataKey={series.key}
                  name={series.label}
                  stackId="requests"
                  fill={series.color}
                  maxBarSize={24}
                  isAnimationActive={false}
                  shape={segmentShape(series.key)}
                />
              ))}
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>
    </ChartCard>
  )
}
