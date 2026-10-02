import { z } from 'zod'
import type { ApiDto } from '../../api/types'
import { Checkbox, TextField } from '../../components/Field'
import { FormDialog } from '../../components/FormDialog'
import { useCreateApi, useUpdateApi } from './queries'

const schema = z.object({
  name: z.string().trim().min(1, 'Name is required').max(100),
  slug: z
    .string()
    .trim()
    .max(64)
    .regex(/^([a-z0-9]+(-[a-z0-9]+)*)?$/, 'Lowercase letters, digits and single hyphens only'),
  description: z.string().max(1000),
  targetBaseUrl: z.url({ protocol: /^https?$/, error: 'Enter an http(s) URL' }),
  isActive: z.boolean(),
})

type Values = z.infer<typeof schema>

export function ApiFormDialog({ api, onClose, onCreated }: { api?: ApiDto; onClose: () => void; onCreated?: (api: ApiDto) => void }) {
  const create = useCreateApi()
  const update = useUpdateApi(api?.id ?? '')

  const save = async ({ slug, description, ...values }: Values) => {
    const shared = { ...values, description: description || null }
    if (api) {
      await update.mutateAsync(shared)
    } else {
      onCreated?.(await create.mutateAsync({ ...shared, slug: slug || null }))
    }
  }

  return (
    <FormDialog
      title={api ? 'Edit API' : 'Register an API'}
      submitLabel={api ? 'Save changes' : 'Register API'}
      schema={schema}
      defaultValues={{
        name: api?.name ?? '',
        slug: api?.slug ?? '',
        description: api?.description ?? '',
        targetBaseUrl: api?.targetBaseUrl ?? '',
        isActive: api?.isActive ?? true,
      }}
      onSubmit={save}
      onClose={onClose}
    >
      {({ register, formState: { errors } }) => (
        <>
          <TextField label="Name" placeholder="Weather API" error={errors.name?.message} {...register('name')} />
          {!api && (
            <TextField
              label="Slug (optional)"
              placeholder="weather-api"
              hint="Used in the gateway URL: /gw/<slug>/… Derived from the name if left empty."
              error={errors.slug?.message}
              {...register('slug')}
            />
          )}
          <TextField
            label="Target base URL"
            placeholder="https://api.example.com/v1"
            hint="Requests to the gateway are forwarded here."
            error={errors.targetBaseUrl?.message}
            {...register('targetBaseUrl')}
          />
          <TextField label="Description (optional)" error={errors.description?.message} {...register('description')} />
          {api && <Checkbox label="Active (accepting requests)" {...register('isActive')} />}
        </>
      )}
    </FormDialog>
  )
}
