import type { ReactNode } from 'react'
import { StatCard } from '../../components/Display'
import { formatCompact, formatCredits, formatMs, formatNumber, formatPercent } from '../../lib/format'
import { ConsumerBars } from './ConsumerBars'
import { ConsumerUsageTable } from './ConsumerUsageTable'
import { LatencyHistogram } from './LatencyHistogram'
import { OutcomeDonut } from './OutcomeDonut'
import type { Audience, UsageData } from './queries'
import { RankedBars } from './RankedBars'
import { byRequests, creditsTrend, latencyTrend, successRate, successRateTrend } from './series'
import { TrafficHeatmap } from './TrafficHeatmap'
import { TrendChart } from './TrendChart'
import { UsageChart } from './UsageChart'

type ViewProps = { data: UsageData; audience: Audience }

const Grid = ({ children }: { children: ReactNode }) => <div className="grid gap-4 lg:grid-cols-2">{children}</div>
const Stack = ({ children }: { children: ReactNode }) => <div className="flex flex-col gap-4">{children}</div>
const Tiles = ({ children }: { children: ReactNode }) => <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">{children}</div>

const creditsLabel = (audience: Audience) => (audience === 'owner' ? 'Credits consumed' : 'Credits spent')

function SuccessRateTile({ data }: { data: UsageData }) {
  const rate = successRate(data.report.summary)
  return <StatCard label="Success rate" value={rate === null ? '–' : formatPercent(rate)} note="Of forwarded requests" />
}

function LatencyTile({ data }: { data: UsageData }) {
  const { summary, averageLatencyMs } = data.report
  return <StatCard label="Average latency" value={summary.successful + summary.failed === 0 ? '–' : formatMs(averageLatencyMs)} />
}

function SuccessRateChart({ data }: { data: UsageData }) {
  return (
    <TrendChart
      title="Success rate"
      hint="Share of forwarded requests the upstream answered without an error."
      points={successRateTrend(data.report.series)}
      bucketMinutes={data.report.bucketMinutes}
      valueLabel="success rate"
      format={formatPercent}
      domain={[0, 100]}
    />
  )
}

export function OverviewView({ data, audience }: ViewProps) {
  const { summary } = data.report

  return (
    <Stack>
      <Tiles>
        <StatCard label="Total requests" value={formatCompact(summary.totalRequests)} />
        <StatCard label="Successful" value={formatCompact(summary.successful)} />
        <StatCard label="Failed" value={formatCompact(summary.failed)} note="Upstream error or unreachable" />
        <StatCard label="Rate limited" value={formatCompact(summary.rateLimited)} />
        <StatCard
          label="Over quota or out of credits"
          value={formatCompact(summary.quotaExceeded + summary.insufficientCredits)}
          note={`${formatNumber(summary.quotaExceeded)} quota · ${formatNumber(summary.insufficientCredits)} credits`}
        />
        <SuccessRateTile data={data} />
        <LatencyTile data={data} />
        <StatCard label={creditsLabel(audience)} value={formatCredits(summary.creditsConsumed)} />
      </Tiles>
      <UsageChart report={data.report} />
      <Grid>
        <OutcomeDonut summary={summary} />
        <SuccessRateChart data={data} />
      </Grid>
    </Stack>
  )
}

export function TrafficView({ data }: ViewProps) {
  const { breakdown } = data.report

  return (
    <Stack>
      <UsageChart report={data.report} />
      <Grid>
        <TrafficHeatmap cells={breakdown.heatmap} />
        <RankedBars title="Top endpoints" hint="Paths with numeric or id segments grouped as {id}." items={byRequests(breakdown.endpoints)} mono />
        <RankedBars title="HTTP methods" items={byRequests(breakdown.methods)} mono />
        <RankedBars title="Requests by API" items={byRequests(breakdown.apis)} />
        <RankedBars title="Requests by tier" items={byRequests(breakdown.tiers)} />
      </Grid>
    </Stack>
  )
}

export function PerformanceView({ data }: ViewProps) {
  const { summary, series, breakdown, bucketMinutes } = data.report

  return (
    <Stack>
      <Tiles>
        <SuccessRateTile data={data} />
        <LatencyTile data={data} />
        <StatCard label="Failed" value={formatCompact(summary.failed)} note="Upstream error or unreachable" />
        <StatCard label="Rate limited" value={formatCompact(summary.rateLimited)} />
      </Tiles>
      <Grid>
        <SuccessRateChart data={data} />
        <TrendChart
          title="Average latency"
          hint="Mean upstream response time per period."
          points={latencyTrend(series)}
          bucketMinutes={bucketMinutes}
          valueLabel="average latency"
          format={formatMs}
        />
        <LatencyHistogram buckets={breakdown.latencyBuckets} />
        <RankedBars
          title="Response status codes"
          hint="What clients received, including gateway rejections."
          items={byRequests(breakdown.statusCodes)}
          mono
        />
        <OutcomeDonut summary={summary} />
      </Grid>
    </Stack>
  )
}

export function ConsumersView({ data }: ViewProps) {
  return (
    <Stack>
      <ConsumerBars consumers={data.consumers} />
      <ConsumerUsageTable consumers={data.consumers} />
    </Stack>
  )
}

export function CreditsView({ data, audience }: ViewProps) {
  const { summary, series, bucketMinutes } = data.report
  const charged = summary.successful === 0 ? 0 : summary.creditsConsumed / summary.successful

  return (
    <Stack>
      <Tiles>
        <StatCard label={creditsLabel(audience)} value={formatCredits(summary.creditsConsumed)} />
        <StatCard label="Charged requests" value={formatCompact(summary.successful)} note="Only successful requests cost credits" />
        <StatCard label="Average per request" value={formatCredits(Number(charged.toFixed(4)))} />
        <StatCard label="Refused for lack of credits" value={formatCompact(summary.insufficientCredits)} />
      </Tiles>
      <TrendChart
        title={`${creditsLabel(audience)} over time`}
        hint="Credits charged for successful requests per period."
        points={creditsTrend(series)}
        bucketMinutes={bucketMinutes}
        valueLabel="credits"
        format={formatCredits}
        area
      />
      {audience === 'owner' && (
        <RankedBars
          title="Credits by consumer"
          items={data.consumers
            .map((consumer) => ({ label: consumer.name, value: consumer.usage.creditsConsumed }))
            .filter((item) => item.value > 0)
            .sort((a, b) => b.value - a.value)}
          format={formatCredits}
        />
      )}
    </Stack>
  )
}
