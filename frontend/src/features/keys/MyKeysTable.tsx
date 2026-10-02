import type { MyKeyDto } from '../../api/types'
import { DataTable } from '../../components/DataTable'
import { Code, Meter } from '../../components/Display'
import { EmptyState } from '../../components/Feedback'
import { formatCredits, formatNumber } from '../../lib/format'
import { KeyStatusBadge } from './KeysTab'

export function MyKeysTable({ keys }: { keys: MyKeyDto[] }) {
  return (
    <DataTable
      rows={keys}
      rowKey={(key) => key.id}
      empty={<EmptyState title="No API keys yet">An API owner issues keys to your account's email address.</EmptyState>}
      columns={[
        {
          header: 'Key',
          cell: (key) => (
            <>
              <span className="font-medium">{key.name}</span>
              <span className="block font-mono text-xs text-ink-2">{key.keyPrefix}…</span>
            </>
          ),
        },
        {
          header: 'API',
          cell: (key) => (
            <>
              {key.apiName}
              <span className="block">
                <Code>/gw/{key.apiSlug}</Code>
              </span>
            </>
          ),
        },
        {
          header: 'Tier',
          cell: (key) => (
            <>
              {key.tierName}
              <span className="block text-xs text-ink-2">
                {formatNumber(key.requestsPerMinute)}/min · {formatCredits(key.creditCostPerRequest)} credits/request
              </span>
            </>
          ),
        },
        {
          header: 'Quota used this month',
          cell: (key) => (
            <div className="flex min-w-40 flex-col gap-1">
              <Meter value={key.quotaUsed} max={key.monthlyQuota} label={`Quota used by ${key.name}`} />
              <span className="text-xs text-ink-2 tabular-nums">
                {formatNumber(key.quotaUsed)} of {formatNumber(key.monthlyQuota)} · {formatNumber(key.quotaRemaining)} left
              </span>
            </div>
          ),
        },
        { header: 'Status', cell: (key) => <KeyStatusBadge status={key.status} /> },
      ]}
    />
  )
}
