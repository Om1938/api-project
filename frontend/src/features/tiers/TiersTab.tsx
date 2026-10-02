import { useState } from 'react'
import { z } from 'zod'
import { errorMessage } from '../../api/client'
import type { TierDto } from '../../api/types'
import { Button } from '../../components/Button'
import { DataTable } from '../../components/DataTable'
import { EmptyState, ErrorNote, QueryView } from '../../components/Feedback'
import { TextField } from '../../components/Field'
import { FormDialog } from '../../components/FormDialog'
import { formatCredits, formatNumber } from '../../lib/format'
import { useDeleteTier, useSaveTier, useTiers } from './queries'

const schema = z.object({
  name: z.string().trim().min(1, 'Name is required').max(100),
  requestsPerMinute: z.number('Enter a number').int().min(1, 'At least 1'),
  monthlyQuota: z.number('Enter a number').int().min(1, 'At least 1'),
  creditCostPerRequest: z.number('Enter a number').min(0, 'Cannot be negative'),
})

function TierFormDialog({ apiId, tier, onClose }: { apiId: string; tier?: TierDto; onClose: () => void }) {
  const save = useSaveTier(apiId, tier?.id)

  return (
    <FormDialog
      title={tier ? `Edit tier "${tier.name}"` : 'New tier'}
      submitLabel={tier ? 'Save changes' : 'Create tier'}
      schema={schema}
      defaultValues={{
        name: tier?.name ?? '',
        requestsPerMinute: tier?.requestsPerMinute ?? 60,
        monthlyQuota: tier?.monthlyQuota ?? 10_000,
        creditCostPerRequest: tier?.creditCostPerRequest ?? 1,
      }}
      onSubmit={save.mutateAsync}
      onClose={onClose}
    >
      {({ register, formState: { errors } }) => (
        <>
          <TextField label="Name" placeholder="Free" error={errors.name?.message} {...register('name')} />
          <TextField
            label="Requests per minute"
            type="number"
            min={1}
            error={errors.requestsPerMinute?.message}
            {...register('requestsPerMinute', { valueAsNumber: true })}
          />
          <TextField
            label="Monthly request quota"
            type="number"
            min={1}
            error={errors.monthlyQuota?.message}
            {...register('monthlyQuota', { valueAsNumber: true })}
          />
          <TextField
            label="Credit cost per request"
            type="number"
            min={0}
            step="any"
            hint="Charged only for successful requests. Use 0 for a free tier."
            error={errors.creditCostPerRequest?.message}
            {...register('creditCostPerRequest', { valueAsNumber: true })}
          />
        </>
      )}
    </FormDialog>
  )
}

export function TiersTab({ apiId }: { apiId: string }) {
  const tiers = useTiers(apiId)
  const deleteTier = useDeleteTier(apiId)
  const [dialog, setDialog] = useState<{ tier?: TierDto } | null>(null)

  const remove = (tier: TierDto) => {
    if (window.confirm(`Delete the tier "${tier.name}"?`)) {
      deleteTier.mutate(tier.id)
    }
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center justify-between gap-4">
        <p className="text-sm text-ink-2">A tier sets how fast and how much a key may call this API, and what each call costs.</p>
        <Button variant="primary" onClick={() => setDialog({})}>
          New tier
        </Button>
      </div>

      {deleteTier.isError && <ErrorNote message={errorMessage(deleteTier.error)} />}

      <QueryView query={tiers}>
        {(rows) => (
          <DataTable
            rows={rows}
            rowKey={(tier) => tier.id}
            empty={<EmptyState title="No tiers yet">Create a tier before issuing keys.</EmptyState>}
            columns={[
              { header: 'Tier', cell: (tier) => <span className="font-medium">{tier.name}</span> },
              { header: 'Requests / minute', numeric: true, cell: (tier) => formatNumber(tier.requestsPerMinute) },
              { header: 'Monthly quota', numeric: true, cell: (tier) => formatNumber(tier.monthlyQuota) },
              { header: 'Credits / request', numeric: true, cell: (tier) => formatCredits(tier.creditCostPerRequest) },
              {
                header: '',
                cell: (tier) => (
                  <div className="flex justify-end gap-2">
                    <Button size="sm" onClick={() => setDialog({ tier })}>
                      Edit
                    </Button>
                    <Button size="sm" variant="danger" onClick={() => remove(tier)}>
                      Delete
                    </Button>
                  </div>
                ),
              },
            ]}
          />
        )}
      </QueryView>

      {dialog && <TierFormDialog apiId={apiId} tier={dialog.tier} onClose={() => setDialog(null)} />}
    </div>
  )
}
