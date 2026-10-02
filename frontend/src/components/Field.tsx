import { useId, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes } from 'react'

const control =
  'rounded-md border border-line bg-surface px-3 py-2 text-sm text-ink placeholder:text-muted focus:border-accent focus:outline-2 focus:outline-accent-soft'

type FieldProps = {
  label: string
  hint?: string
  error?: string
  children: (id: string) => ReactNode
}

export function Field({ label, hint, error, children }: FieldProps) {
  const id = useId()
  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={id} className="text-sm font-medium text-ink">
        {label}
      </label>
      {children(id)}
      {error ? (
        <p role="alert" className="text-xs text-critical">
          {error}
        </p>
      ) : (
        hint && <p className="text-xs text-ink-2">{hint}</p>
      )}
    </div>
  )
}

type TextFieldProps = InputHTMLAttributes<HTMLInputElement> & { label: string; hint?: string; error?: string }

export function TextField({ label, hint, error, ...input }: TextFieldProps) {
  return (
    <Field label={label} hint={hint} error={error}>
      {(id) => <input id={id} className={`${control} w-full`} aria-invalid={Boolean(error)} {...input} />}
    </Field>
  )
}

type SelectFieldProps = SelectHTMLAttributes<HTMLSelectElement> & { label: string; hint?: string; error?: string }

export function SelectField({ label, hint, error, children, ...select }: SelectFieldProps) {
  return (
    <Field label={label} hint={hint} error={error}>
      {(id) => (
        <select id={id} className={`${control} w-full`} aria-invalid={Boolean(error)} {...select}>
          {children}
        </select>
      )}
    </Field>
  )
}

type CheckboxProps = InputHTMLAttributes<HTMLInputElement> & { label: string }

export function Checkbox({ label, ...input }: CheckboxProps) {
  return (
    <label className="flex items-center gap-2 text-sm text-ink">
      <input type="checkbox" className="size-4 accent-(--accent)" {...input} />
      {label}
    </label>
  )
}

export function InlineSelect({ className = '', ...select }: SelectHTMLAttributes<HTMLSelectElement>) {
  return <select className={`${control} ${className}`} {...select} />
}
