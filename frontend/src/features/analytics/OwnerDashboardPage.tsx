import { useState } from 'react'
import { PageHeader, Section } from '../../components/Display'
import { QueryView } from '../../components/Feedback'
import { InlineSelect } from '../../components/Field'
import { useApis } from '../apis/queries'
import { ConsumerBars } from './ConsumerBars'
import { useOwnerAnalytics } from './queries'
import { DEFAULT_RANGE } from './range'
import { ConsumerUsageTable, RangeSelect, UsageReport } from './UsageReport'

export function OwnerAnalytics({ apiId, filters }: { apiId?: string; filters?: (range: React.ReactNode) => React.ReactNode }) {
  const [range, setRange] = useState(DEFAULT_RANGE)
  const analytics = useOwnerAnalytics(range, apiId)
  const rangeSelect = <RangeSelect value={range} onChange={setRange} />

  return (
    <>
      <div className="mb-4 flex flex-wrap items-center gap-2">{filters ? filters(rangeSelect) : rangeSelect}</div>
      <QueryView query={analytics}>
        {(data) => (
          <>
            <UsageReport report={data.report} creditsLabel="Credits consumed">
              <ConsumerBars consumers={data.consumers} />
            </UsageReport>
            <Section title="Usage per consumer">
              <ConsumerUsageTable consumers={data.consumers} />
            </Section>
          </>
        )}
      </QueryView>
    </>
  )
}

export function OwnerDashboardPage() {
  const apis = useApis()
  const [apiId, setApiId] = useState('')

  return (
    <>
      <PageHeader title="Overview" description="Traffic through the gateway for your APIs. Refreshes every few seconds." />
      <OwnerAnalytics
        apiId={apiId || undefined}
        filters={(range) => (
          <>
            {range}
            <InlineSelect aria-label="API" value={apiId} onChange={(event) => setApiId(event.target.value)}>
              <option value="">All APIs</option>
              {apis.data?.map((api) => (
                <option key={api.id} value={api.id}>
                  {api.name}
                </option>
              ))}
            </InlineSelect>
          </>
        )}
      />
    </>
  )
}
