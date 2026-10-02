import type { ConsumerUsageDto } from '../../api/types'
import { DataTable } from '../../components/DataTable'
import { EmptyState } from '../../components/Feedback'
import { formatCredits, formatNumber } from '../../lib/format'

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
