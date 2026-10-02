import type { ReactNode } from 'react'
import type { ConsumerUsageDto, UsageReportDto } from '../../api/types'
import { DataTable } from '../../components/DataTable'
import { StatCard } from '../../components/Display'
import { EmptyState } from '../../components/Feedback'
import { InlineSelect } from '../../components/Field'
import { formatCompact, formatCredits, formatMs, formatNumber, formatPercent } from '../../lib/format'
import { LatencyHistogram } from './LatencyHistogram'
import { OutcomeDonut } from './OutcomeDonut'
import { RANGE_PRESETS, type RangePreset } from './range'
import { RankedBars } from './RankedBars'
import { creditsTrend, latencyTrend, successRate, successRateTrend } from './series'
import { TrafficHeatmap } from './TrafficHeatmap'
import { TrendChart } from './TrendChart'
import { UsageChart } from './UsageChart'

export function RangeSelect({ value, onChange }: { value: RangePreset; onChange: (value: RangePreset) => void }) {
  return (
    <InlineSelect aria-label="Time range" value={value} onChange={(event) => onChange(event.target.value as RangePreset)}>
      {RANGE_PRESETS.map((preset) => (
        <option key={preset.value} value={preset.value}>
          {preset.label}
        </option>
      ))}
    </InlineSelect>
  )
}

type Props = {
  report: UsageReportDto
  creditsLabel: string
  children?: ReactNode
}

export function UsageReport({ report, creditsLabel, children }: Props) {
  const { summary, series, breakdown, bucketMinutes } = report
  const rate = successRate(summary)

  return (
    <div className="flex flex-col gap-4">
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <StatCard label="Total requests" value={formatCompact(summary.totalRequests)} />
        <StatCard label="Successful" value={formatCompact(summary.successful)} />
        <StatCard label="Failed" value={formatCompact(summary.failed)} note="Upstream error or unreachable" />
        <StatCard label="Rate limited" value={formatCompact(summary.rateLimited)} />
        <StatCard
          label="Over quota or out of credits"
          value={formatCompact(summary.quotaExceeded + summary.insufficientCredits)}
          note={`${formatNumber(summary.quotaExceeded)} quota · ${formatNumber(summary.insufficientCredits)} credits`}
        />
        <StatCard label="Success rate" value={rate === null ? '–' : formatPercent(rate)} note="Of forwarded requests" />
        <StatCard label="Average latency" value={summary.successful + summary.failed === 0 ? '–' : formatMs(report.averageLatencyMs)} />
        <StatCard label={creditsLabel} value={formatCredits(summary.creditsConsumed)} />
      </div>

      <UsageChart report={report} />

      <div className="grid gap-4 lg:grid-cols-2">
        <OutcomeDonut summary={summary} />
        <TrendChart
          title="Success rate"
          hint="Share of forwarded requests the upstream answered without an error."
          points={successRateTrend(series)}
          bucketMinutes={bucketMinutes}
          valueLabel="success rate"
          format={formatPercent}
          domain={[0, 100]}
        />
        <TrendChart
          title="Average latency"
          hint="Mean upstream response time per period."
          points={latencyTrend(series)}
          bucketMinutes={bucketMinutes}
          valueLabel="average latency"
          format={formatMs}
        />
        <LatencyHistogram buckets={breakdown.latencyBuckets} />
        <TrafficHeatmap cells={breakdown.heatmap} />
        <TrendChart
          title={creditsLabel}
          hint="Credits charged for successful requests per period."
          points={creditsTrend(series)}
          bucketMinutes={bucketMinutes}
          valueLabel="credits"
          format={formatCredits}
          area
        />
        <RankedBars title="Top endpoints" hint="Paths with numeric or id segments grouped as {id}." items={breakdown.endpoints} mono />
        <RankedBars title="Response status codes" hint="What clients received, including gateway rejections." items={breakdown.statusCodes} mono />
        <RankedBars title="HTTP methods" items={breakdown.methods} mono />
        <RankedBars title="Requests by API" items={breakdown.apis} />
        <RankedBars title="Requests by tier" items={breakdown.tiers} />
        {children}
      </div>
    </div>
  )
}

export function ConsumerUsageTable({ consumers }: { consumers: ConsumerUsageDto[] }) {
  return (
    <DataTable
      rows={consumers}
      rowKey={(consumer) => consumer.consumerId}
      empty={<EmptyState title="No consumer activity in this period" />}
      columns={[
        {
          header: 'Consumer',
          cell: (consumer) => (
            <>
              <span className="font-medium">{consumer.name}</span>
              <span className="block text-xs text-ink-2">{consumer.email}</span>
            </>
          ),
        },
        { header: 'Requests', numeric: true, cell: (consumer) => formatNumber(consumer.usage.totalRequests) },
        { header: 'Successful', numeric: true, cell: (consumer) => formatNumber(consumer.usage.successful) },
        { header: 'Failed', numeric: true, cell: (consumer) => formatNumber(consumer.usage.failed) },
        { header: 'Rate limited', numeric: true, cell: (consumer) => formatNumber(consumer.usage.rateLimited) },
        { header: 'Credits', numeric: true, cell: (consumer) => formatCredits(consumer.usage.creditsConsumed) },
      ]}
    />
  )
}
