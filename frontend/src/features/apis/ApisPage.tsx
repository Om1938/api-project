import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Button } from '../../components/Button'
import { DataTable } from '../../components/DataTable'
import { Badge, Code, PageHeader } from '../../components/Display'
import { EmptyState, QueryView } from '../../components/Feedback'
import { ApiFormDialog } from './ApiFormDialog'
import { useApis } from './queries'

export function ApiStatusBadge({ isActive }: { isActive: boolean }) {
  return <Badge tone={isActive ? 'good' : 'neutral'}>{isActive ? 'Active' : 'Disabled'}</Badge>
}

export function ApisPage() {
  const apis = useApis()
  const navigate = useNavigate()
  const [creating, setCreating] = useState(false)

  return (
    <>
      <PageHeader
        title="APIs"
        description="Upstream APIs you expose through the gateway."
        actions={
          <Button variant="primary" onClick={() => setCreating(true)}>
            Register API
          </Button>
        }
      />

      <QueryView query={apis}>
        {(rows) => (
          <DataTable
            rows={rows}
            rowKey={(api) => api.id}
            empty={
              <EmptyState title="No APIs yet">Register your first API to start issuing keys for it.</EmptyState>
            }
            columns={[
              {
                header: 'Name',
                cell: (api) => (
                  <Link to={api.id} className="font-medium text-accent hover:underline">
                    {api.name}
                  </Link>
                ),
              },
              { header: 'Gateway path', cell: (api) => <Code>/gw/{api.slug}</Code> },
              { header: 'Forwards to', cell: (api) => <span className="break-all text-ink-2">{api.targetBaseUrl}</span> },
              { header: 'Status', cell: (api) => <ApiStatusBadge isActive={api.isActive} /> },
            ]}
          />
        )}
      </QueryView>

      {creating && <ApiFormDialog onClose={() => setCreating(false)} onCreated={(api) => navigate(api.id)} />}
    </>
  )
}
