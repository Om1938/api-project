import { useState } from 'react'
import { z } from 'zod'
import type { WebhookDto } from '../../api/types'
import { Button } from '../../components/Button'
import { DataTable } from '../../components/DataTable'
import { Badge, Code, CopyButton, Section } from '../../components/Display'
import { EmptyState, QueryView } from '../../components/Feedback'
import { Checkbox, TextField } from '../../components/Field'
import { FormDialog } from '../../components/FormDialog'
import { formatDateTime } from '../../lib/format'
import { eventLabel, useDeleteWebhook, useDeliveries, useSaveWebhook, useWebhooks, WEBHOOK_EVENTS } from './queries'

const schema = z.object({
  url: z.url({ protocol: /^https?$/, error: 'Enter an http(s) URL' }),
  thresholdPercent: z.number('Enter a number').int().min(1, 'Between 1 and 100').max(100, 'Between 1 and 100'),
  events: z.array(z.string()).min(1, 'Choose at least one event'),
  isActive: z.boolean(),
})

function WebhookFormDialog({ apiId, webhook, onClose }: { apiId: string; webhook?: WebhookDto; onClose: () => void }) {
  const save = useSaveWebhook(apiId, webhook?.id)

  return (
    <FormDialog
      title={webhook ? 'Edit webhook' : 'New webhook'}
      submitLabel={webhook ? 'Save changes' : 'Create webhook'}
      schema={schema}
      defaultValues={{
        url: webhook?.url ?? '',
        thresholdPercent: webhook?.thresholdPercent ?? 80,
        events: webhook?.events ?? WEBHOOK_EVENTS.map((event) => event.name),
        isActive: webhook?.isActive ?? true,
      }}
      onSubmit={save.mutateAsync}
      onClose={onClose}
    >
      {({ register, formState: { errors } }) => (
        <>
          <TextField label="Endpoint URL" placeholder="https://example.com/hooks/gateway" error={errors.url?.message} {...register('url')} />
          <fieldset className="flex flex-col gap-2">
            <legend className="mb-1 text-sm font-medium text-ink">Events</legend>
            {WEBHOOK_EVENTS.map((event) => (
              <Checkbox key={event.name} label={event.label} value={event.name} {...register('events')} />
            ))}
            {errors.events?.message && (
              <p role="alert" className="text-xs text-critical">
                {errors.events.message}
              </p>
            )}
          </fieldset>
          <TextField
            label="Quota threshold (%)"
            type="number"
            min={1}
            max={100}
            hint="“Quota threshold reached” fires when a key has used this share of its monthly quota."
            error={errors.thresholdPercent?.message}
            {...register('thresholdPercent', { valueAsNumber: true })}
          />
          <Checkbox label="Active" {...register('isActive')} />
        </>
      )}
    </FormDialog>
  )
}

export function WebhooksTab({ apiId }: { apiId: string }) {
  const webhooks = useWebhooks(apiId)
  const deliveries = useDeliveries(apiId)
  const deleteWebhook = useDeleteWebhook(apiId)
  const [dialog, setDialog] = useState<{ webhook?: WebhookDto } | null>(null)

  const remove = (webhook: WebhookDto) => {
    if (window.confirm(`Delete the webhook to ${webhook.url}?`)) {
      deleteWebhook.mutate(webhook.id)
    }
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center justify-between gap-4">
        <p className="text-sm text-ink-2">
          Get an HTTP POST when a key approaches or exceeds its monthly quota. Each request is signed with the secret (
          <Code>X-Gateway-Signature</Code>).
        </p>
        <Button variant="primary" onClick={() => setDialog({})}>
          New webhook
        </Button>
      </div>

      <QueryView query={webhooks}>
        {(rows) => (
          <DataTable
            rows={rows}
            rowKey={(webhook) => webhook.id}
            empty={<EmptyState title="No webhooks configured" />}
            columns={[
              { header: 'Endpoint', cell: (webhook) => <span className="break-all">{webhook.url}</span> },
              { header: 'Events', cell: (webhook) => webhook.events.map(eventLabel).join(', ') },
              { header: 'Threshold', numeric: true, cell: (webhook) => `${webhook.thresholdPercent}%` },
              {
                header: 'Status',
                cell: (webhook) => <Badge tone={webhook.isActive ? 'good' : 'neutral'}>{webhook.isActive ? 'Active' : 'Paused'}</Badge>,
              },
              {
                header: '',
                cell: (webhook) => (
                  <div className="flex justify-end gap-2">
                    <CopyButton text={webhook.secret} label="Copy secret" />
                    <Button size="sm" onClick={() => setDialog({ webhook })}>
                      Edit
                    </Button>
                    <Button size="sm" variant="danger" onClick={() => remove(webhook)}>
                      Delete
                    </Button>
                  </div>
                ),
              },
            ]}
          />
        )}
      </QueryView>

      <Section title="Recent deliveries">
        <QueryView query={deliveries}>
          {(rows) => (
            <DataTable
              rows={rows}
              rowKey={(delivery) => delivery.id}
              empty={<EmptyState title="Nothing delivered yet">Deliveries appear here when a quota event fires.</EmptyState>}
              columns={[
                { header: 'When', cell: (delivery) => <span className="text-ink-2">{formatDateTime(delivery.createdAt)}</span> },
                { header: 'Event', cell: (delivery) => eventLabel(delivery.eventType) },
                { header: 'Endpoint', cell: (delivery) => <span className="break-all text-ink-2">{delivery.url}</span> },
                {
                  header: 'Result',
                  cell: (delivery) => (
                    <Badge tone={delivery.success ? 'good' : 'critical'}>
                      {delivery.success ? `Delivered (${delivery.responseStatus})` : `Failed: ${delivery.error ?? 'unknown error'}`}
                    </Badge>
                  ),
                },
                { header: 'Attempts', numeric: true, cell: (delivery) => delivery.attempts },
              ]}
            />
          )}
        </QueryView>
      </Section>

      {dialog && <WebhookFormDialog apiId={apiId} webhook={dialog.webhook} onClose={() => setDialog(null)} />}
    </div>
  )
}
