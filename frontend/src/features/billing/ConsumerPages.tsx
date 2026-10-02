import { useState } from 'react'
import { Link } from 'react-router-dom'
import { errorMessage } from '../../api/client'
import { Button } from '../../components/Button'
import { DataTable } from '../../components/DataTable'
import { Card, Code, PageHeader, Section, StatCard } from '../../components/Display'
import { EmptyState, ErrorNote, QueryView } from '../../components/Feedback'
import { formatCredits, formatDateTime } from '../../lib/format'
import { API_KEY_HEADER, gatewayUrl } from '../../lib/gateway'
import { useMyUsage } from '../analytics/queries'
import { DEFAULT_RANGE } from '../analytics/range'
import { RangeSelect, UsageReport } from '../analytics/UsageReport'
import { MyKeysTable } from '../keys/MyKeysTable'
import { useMyKeys } from '../keys/queries'
import { useCreditAccount, useTopUp } from './queries'

const TOP_UP_AMOUNTS = [50, 100, 500]

export function ConsumerDashboardPage() {
  const [range, setRange] = useState(DEFAULT_RANGE)
  const usage = useMyUsage(range)
  const credits = useCreditAccount()
  const keys = useMyKeys()

  return (
    <>
      <PageHeader title="Overview" description="Your usage, remaining quota and credits. Refreshes every few seconds." />

      <div className="mb-4 flex flex-wrap items-center gap-2">
        <RangeSelect value={range} onChange={setRange} />
      </div>

      <QueryView query={credits}>
        {(account) => (
          <div className="mb-4 grid grid-cols-2 gap-3">
            <StatCard label="Credit balance" value={formatCredits(account.balance)} />
            <StatCard label="Credits spent this month" value={formatCredits(account.spentThisMonth)} />
          </div>
        )}
      </QueryView>

      <QueryView query={usage}>{(report) => <UsageReport report={report} creditsLabel="Credits spent" />}</QueryView>

      <Section title="Quota per key" actions={<Link to="keys" className="text-sm text-accent hover:underline">All keys</Link>}>
        <QueryView query={keys}>{(rows) => <MyKeysTable keys={rows} />}</QueryView>
      </Section>
    </>
  )
}

export function MyKeysPage() {
  const keys = useMyKeys()

  return (
    <>
      <PageHeader title="My API keys" description="Keys that API owners have issued to you." />
      <QueryView query={keys}>{(rows) => <MyKeysTable keys={rows} />}</QueryView>

      <Section title="How to call an API">
        <Card>
          <p className="text-sm text-ink-2">
            Send requests to the API's gateway path and put your key in the <Code>{API_KEY_HEADER}</Code> header. The full key was given
            to you by the API owner when it was created; only its first characters are shown here.
          </p>
          <p className="mt-3">
            <Code>
              curl -i "{gatewayUrl('<api-slug>', '/<path>')}" -H "{API_KEY_HEADER}: &lt;your key&gt;"
            </Code>
          </p>
        </Card>
      </Section>
    </>
  )
}

export function CreditsPage() {
  const account = useCreditAccount()
  const topUp = useTopUp()

  return (
    <>
      <PageHeader title="Credits" description="Each successful request deducts credits according to the key's tier." />

      <QueryView query={account}>
        {(data) => (
          <>
            <div className="grid gap-4 md:grid-cols-2">
              <Card title="Balance">
                <p className="text-5xl font-semibold text-ink">{formatCredits(data.balance)}</p>
                <p className="mt-2 text-sm text-ink-2">{formatCredits(data.spentThisMonth)} credits spent this month</p>
              </Card>

              <Card title="Add credits">
                <p className="mb-3 text-sm text-ink-2">This is a mock purchase. No payment is taken.</p>
                <div className="flex flex-wrap gap-2">
                  {TOP_UP_AMOUNTS.map((amount) => (
                    <Button key={amount} disabled={topUp.isPending} onClick={() => topUp.mutate({ amount })}>
                      + {amount} credits
                    </Button>
                  ))}
                </div>
                {topUp.isError && (
                  <div className="mt-3">
                    <ErrorNote message={errorMessage(topUp.error)} />
                  </div>
                )}
              </Card>
            </div>

            <Section title="Recent top-ups">
              <DataTable
                rows={data.transactions}
                rowKey={(transaction) => transaction.id}
                empty={<EmptyState title="No top-ups yet" />}
                columns={[
                  { header: 'When', cell: (transaction) => <span className="text-ink-2">{formatDateTime(transaction.createdAt)}</span> },
                  { header: 'Description', cell: (transaction) => transaction.description },
                  { header: 'Credits', numeric: true, cell: (transaction) => `+${formatCredits(transaction.amount)}` },
                ]}
              />
            </Section>
          </>
        )}
      </QueryView>
    </>
  )
}
