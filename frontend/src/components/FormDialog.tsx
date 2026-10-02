import { zodResolver } from '@hookform/resolvers/zod'
import { useState, type ReactNode } from 'react'
import { useForm, type DefaultValues, type FieldValues, type Resolver, type UseFormReturn } from 'react-hook-form'
import type { ZodType } from 'zod'
import { errorMessage } from '../api/client'
import { Button } from './Button'
import { ErrorNote } from './Feedback'
import { Modal } from './Modal'

type Props<T extends FieldValues> = {
  title: string
  submitLabel: string
  schema: ZodType<T>
  defaultValues: DefaultValues<T>
  onSubmit: (values: T) => Promise<unknown>
  onClose: () => void
  children: (form: UseFormReturn<T>) => ReactNode
}

export function FormDialog<T extends FieldValues>({
  title,
  submitLabel,
  schema,
  defaultValues,
  onSubmit,
  onClose,
  children,
}: Props<T>) {
  const form = useForm<T>({ resolver: zodResolver(schema as never) as Resolver<T>, defaultValues })
  const [serverError, setServerError] = useState<string | null>(null)

  const submit = form.handleSubmit(async (values) => {
    setServerError(null)
    try {
      await onSubmit(values)
      onClose()
    } catch (error) {
      setServerError(errorMessage(error))
    }
  })

  return (
    <Modal title={title} onClose={onClose}>
      <form onSubmit={submit} className="flex flex-col gap-4" noValidate>
        {children(form)}
        {serverError && <ErrorNote message={serverError} />}
        <div className="mt-2 flex justify-end gap-2">
          <Button onClick={onClose}>Cancel</Button>
          <Button type="submit" variant="primary" disabled={form.formState.isSubmitting}>
            {form.formState.isSubmitting ? 'Saving…' : submitLabel}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
