import { DataTable } from '../../components/DataTable'
import { PageHeader } from '../../components/Display'
import { EmptyState, QueryView } from '../../components/Feedback'
import { formatDateTime, formatNumber } from '../../lib/format'
import { useConsumers } from './queries'

export function ConsumersPage() {
  const consumers = useConsumers()

  return (
    <>
      <PageHeader title="Consumers" description="Registered consumer accounts and the active keys they hold on your APIs." />
      <QueryView query={consumers}>
        {(rows) => (
          <DataTable
            rows={rows}
            rowKey={(consumer) => consumer.id}
            empty={<EmptyState title="No consumers yet">Consumers show up here after they register an account.</EmptyState>}
            columns={[
              { header: 'Name', cell: (consumer) => <span className="font-medium">{consumer.name}</span> },
              { header: 'Email', cell: (consumer) => consumer.email },
              { header: 'Active keys on your APIs', numeric: true, cell: (consumer) => formatNumber(consumer.activeKeyCount) },
              { header: 'Registered', cell: (consumer) => <span className="text-ink-2">{formatDateTime(consumer.createdAt)}</span> },
            ]}
          />
        )}
      </QueryView>
    </>
  )
}
