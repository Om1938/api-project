import { useState } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import type { ApiDto } from '../../api/types'
import { Button } from '../../components/Button'
import { Code, CopyButton, PageHeader } from '../../components/Display'
import { QueryView } from '../../components/Feedback'
import { gatewayUrl } from '../../lib/gateway'
import { ApiUsage } from '../analytics/AnalyticsPage'
import { KeysTab } from '../keys/KeysTab'
import { TiersTab } from '../tiers/TiersTab'
import { WebhooksTab } from '../webhooks/WebhooksTab'
import { ApiFormDialog } from './ApiFormDialog'
import { ApiStatusBadge } from './ApisPage'
import { useApi, useDeleteApi } from './queries'

const TABS = [
  { id: 'tiers', label: 'Tiers', render: (api: ApiDto) => <TiersTab apiId={api.id} /> },
  { id: 'keys', label: 'API keys', render: (api: ApiDto) => <KeysTab api={api} /> },
  { id: 'webhooks', label: 'Webhooks', render: (api: ApiDto) => <WebhooksTab apiId={api.id} /> },
  { id: 'usage', label: 'Usage', render: (api: ApiDto) => <ApiUsage apiId={api.id} /> },
] as const

export function ApiDetailPage() {
  const { apiId = '' } = useParams()
  const api = useApi(apiId)

  return (
    <>
      <Link to=".." relative="path" className="text-sm text-ink-2 hover:text-ink">
        ← All APIs
      </Link>
      <div className="mt-3">
        <QueryView query={api}>{(data) => <ApiDetail api={data} />}</QueryView>
      </div>
    </>
  )
}

function ApiDetail({ api }: { api: ApiDto }) {
  const navigate = useNavigate()
  const deleteApi = useDeleteApi()
  const [editing, setEditing] = useState(false)
  const [searchParams, setSearchParams] = useSearchParams()
  const activeTab = TABS.find((tab) => tab.id === searchParams.get('tab')) ?? TABS[0]

  const remove = async () => {
    if (window.confirm(`Delete "${api.name}" with all its tiers, keys and webhooks? This cannot be undone.`)) {
      await deleteApi.mutateAsync(api.id)
      navigate('..', { relative: 'path' })
    }
  }

  return (
    <>
      <PageHeader
        title={api.name}
        description={api.description ?? undefined}
        actions={
          <>
            <Button onClick={() => setEditing(true)}>Edit</Button>
            <Button variant="danger" onClick={remove} disabled={deleteApi.isPending}>
              Delete
            </Button>
          </>
        }
      />

      <dl className="mb-6 grid gap-x-8 gap-y-3 rounded-lg border border-line bg-surface p-5 text-sm sm:grid-cols-[auto_1fr]">
        <dt className="text-ink-2">Status</dt>
        <dd>
          <ApiStatusBadge isActive={api.isActive} />
        </dd>
        <dt className="text-ink-2">Gateway URL</dt>
        <dd className="flex flex-wrap items-center gap-2">
          <Code>{gatewayUrl(api.slug)}</Code>
          <CopyButton text={gatewayUrl(api.slug)} />
        </dd>
        <dt className="text-ink-2">Forwards to</dt>
        <dd>
          <Code>{api.targetBaseUrl}</Code>
        </dd>
      </dl>

      <div role="tablist" className="mb-5 flex gap-1 border-b border-line">
        {TABS.map((tab) => (
          <button
            key={tab.id}
            role="tab"
            type="button"
            aria-selected={tab.id === activeTab.id}
            onClick={() => setSearchParams({ tab: tab.id }, { replace: true })}
            className={`-mb-px border-b-2 px-3 py-2 text-sm ${
              tab.id === activeTab.id ? 'border-accent font-medium text-ink' : 'border-transparent text-ink-2 hover:text-ink'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      <div role="tabpanel">{activeTab.render(api)}</div>

      {editing && <ApiFormDialog api={api} onClose={() => setEditing(false)} />}
    </>
  )
}
