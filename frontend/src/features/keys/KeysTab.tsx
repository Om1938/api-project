import { useState } from 'react'
import { z } from 'zod'
import type { ApiDto, ApiKeyDto, CreatedApiKeyDto } from '../../api/types'
import { Button } from '../../components/Button'
import { DataTable } from '../../components/DataTable'
import { Badge, Code, CopyButton } from '../../components/Display'
import { EmptyState, QueryView } from '../../components/Feedback'
import { SelectField, TextField } from '../../components/Field'
import { FormDialog } from '../../components/FormDialog'
import { Modal } from '../../components/Modal'
import { formatDateTime } from '../../lib/format'
import { curlExample } from '../../lib/gateway'
import { useConsumers } from '../consumers/queries'
import { useTiers } from '../tiers/queries'
import { useIssueKey, useKeys, useRevokeKey } from './queries'

export function KeyStatusBadge({ status }: { status: ApiKeyDto['status'] }) {
  return <Badge tone={status === 'Active' ? 'good' : 'critical'}>{status}</Badge>
}

const schema = z.object({
  name: z.string().trim().min(1, 'Name is required').max(100),
  tierId: z.string().min(1, 'Choose a tier'),
  consumerEmail: z.string().min(1, 'Choose a consumer'),
})

function IssueKeyDialog({ apiId, onClose, onIssued }: { apiId: string; onClose: () => void; onIssued: (key: CreatedApiKeyDto) => void }) {
  const tiers = useTiers(apiId)
  const consumers = useConsumers()
  const issue = useIssueKey(apiId)

  return (
    <FormDialog
      title="Issue an API key"
      submitLabel="Issue key"
      schema={schema}
      defaultValues={{ name: '', tierId: '', consumerEmail: '' }}
      onSubmit={async (values) => onIssued(await issue.mutateAsync(values))}
      onClose={onClose}
    >
      {({ register, formState: { errors } }) => (
        <>
          <TextField label="Key name" placeholder="Production" error={errors.name?.message} {...register('name')} />
          <SelectField label="Tier" error={errors.tierId?.message} {...register('tierId')}>
            <option value="">Select a tier…</option>
            {tiers.data?.map((tier) => (
              <option key={tier.id} value={tier.id}>
                {tier.name} — {tier.requestsPerMinute}/min, {tier.monthlyQuota}/month
              </option>
            ))}
          </SelectField>
          <SelectField
            label="Consumer"
            hint="Consumers appear here once they have registered an account."
            error={errors.consumerEmail?.message}
            {...register('consumerEmail')}
          >
            <option value="">Select a consumer…</option>
            {consumers.data?.map((consumer) => (
              <option key={consumer.id} value={consumer.email}>
                {consumer.name} ({consumer.email})
              </option>
            ))}
          </SelectField>
        </>
      )}
    </FormDialog>
  )
}

function IssuedKeyDialog({ api, issued, onClose }: { api: ApiDto; issued: CreatedApiKeyDto; onClose: () => void }) {
  const example = curlExample(api.slug, issued.secret)

  return (
    <Modal title="API key created" onClose={onClose}>
      <div className="flex flex-col gap-4 text-sm">
        <p className="text-ink-2">
          Copy this key and pass it to <span className="font-medium text-ink">{issued.key.consumerName}</span> now. It is stored only as a hash and
          cannot be shown again.
        </p>
        <div className="flex flex-wrap items-center gap-2">
          <Code>{issued.secret}</Code>
          <CopyButton text={issued.secret} label="Copy key" />
        </div>
        <div>
          <p className="mb-1 text-ink-2">Try it:</p>
          <div className="flex flex-wrap items-center gap-2">
            <Code>{example}</Code>
            <CopyButton text={example} label="Copy command" />
          </div>
        </div>
        <div className="flex justify-end">
          <Button variant="primary" onClick={onClose}>
            Done
          </Button>
        </div>
      </div>
    </Modal>
  )
}

export function KeysTab({ api }: { api: ApiDto }) {
  const keys = useKeys(api.id)
  const revoke = useRevokeKey(api.id)
  const [issuing, setIssuing] = useState(false)
  const [issued, setIssued] = useState<CreatedApiKeyDto | null>(null)

  const revokeKey = (key: ApiKeyDto) => {
    if (window.confirm(`Revoke "${key.name}" (${key.keyPrefix}…)? Requests using it will be rejected immediately.`)) {
      revoke.mutate(key.id)
    }
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center justify-between gap-4">
        <p className="text-sm text-ink-2">Each key gives one consumer access to this API at one tier.</p>
        <Button variant="primary" onClick={() => setIssuing(true)}>
          Issue key
        </Button>
      </div>

      <QueryView query={keys}>
        {(rows) => (
          <DataTable
            rows={rows}
            rowKey={(key) => key.id}
            empty={<EmptyState title="No keys issued yet" />}
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
                header: 'Consumer',
                cell: (key) => (
                  <>
                    {key.consumerName}
                    <span className="block text-xs text-ink-2">{key.consumerEmail}</span>
                  </>
                ),
              },
              { header: 'Tier', cell: (key) => key.tierName },
              { header: 'Status', cell: (key) => <KeyStatusBadge status={key.status} /> },
              { header: 'Created', cell: (key) => <span className="text-ink-2">{formatDateTime(key.createdAt)}</span> },
              {
                header: '',
                cell: (key) =>
                  key.status === 'Active' && (
                    <div className="flex justify-end">
                      <Button size="sm" variant="danger" onClick={() => revokeKey(key)}>
                        Revoke
                      </Button>
                    </div>
                  ),
              },
            ]}
          />
        )}
      </QueryView>

      {issuing && <IssueKeyDialog apiId={api.id} onClose={() => setIssuing(false)} onIssued={setIssued} />}
      {issued && <IssuedKeyDialog api={api} issued={issued} onClose={() => setIssued(null)} />}
    </div>
  )
}
