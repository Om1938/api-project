import type { UseQueryResult } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { errorMessage } from '../api/client'

export function ErrorNote({ message }: { message: string }) {
  return (
    <p role="alert" className="rounded-md bg-critical-soft px-3 py-2 text-sm text-critical">
      {message}
    </p>
  )
}

export function EmptyState({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="rounded-lg border border-dashed border-line px-6 py-10 text-center">
      <p className="text-sm font-medium text-ink">{title}</p>
      {children && <div className="mt-1 text-sm text-ink-2">{children}</div>}
    </div>
  )
}

type QueryViewProps<T> = {
  query: UseQueryResult<T>
  children: (data: T) => ReactNode
}

export function QueryView<T>({ query, children }: QueryViewProps<T>) {
  if (query.data !== undefined) {
    return <div className={query.isPlaceholderData ? 'opacity-60 transition-opacity' : undefined}>{children(query.data)}</div>
  }

  if (query.isError) {
    return <ErrorNote message={errorMessage(query.error)} />
  }

  return <p className="py-8 text-center text-sm text-ink-2">Loading…</p>
}
