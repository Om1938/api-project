import { useState, type ReactNode } from 'react'
import { Button } from '../../components/Button'
import { DataTable, type Column } from '../../components/DataTable'
import { Card } from '../../components/Display'
import { USAGE_SERIES } from './series'

type TableView<T> = { columns: Column<T>[]; rows: T[]; rowKey: (row: T) => string }

type Props<T> = {
  title: string
  hint?: string
  isEmpty?: boolean
  table?: TableView<T>
  className?: string
  children: ReactNode
}

export function ChartCard<T>({ title, hint, isEmpty, table, className, children }: Props<T>) {
  const [showTable, setShowTable] = useState(false)

  return (
    <div className={`[&>section]:h-full ${className ?? ''}`}>
      <Card
        title={title}
        actions={
          table &&
          !isEmpty && (
            <Button size="sm" variant="ghost" onClick={() => setShowTable((value) => !value)}>
              {showTable ? 'Show chart' : 'Show table'}
            </Button>
          )
        }
      >
        {hint && <p className="-mt-3 mb-4 text-xs text-ink-2">{hint}</p>}
        {isEmpty ? (
          <p className="py-10 text-center text-sm text-ink-2">No data in this period</p>
        ) : showTable && table ? (
          <div className="max-h-80 overflow-y-auto">
            <DataTable {...table} empty={null} />
          </div>
        ) : (
          children
        )}
      </Card>
    </div>
  )
}

export function SeriesLegend() {
  return (
    <ul className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-ink-2">
      {USAGE_SERIES.map((series) => (
        <li key={series.key} className="flex items-center gap-1.5">
          <span aria-hidden className="size-2.5 rounded-xs" style={{ background: series.color }} />
          {series.label}
        </li>
      ))}
    </ul>
  )
}

export function TooltipBox({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="rounded-md border border-line bg-surface px-3 py-2 text-xs shadow-lg">
      <p className="mb-1.5 text-ink-2">{title}</p>
      {children}
    </div>
  )
}

export function TooltipValue({ value, label, color }: { value: string; label: string; color?: string }) {
  return (
    <p className="flex items-center gap-2">
      {color && <span aria-hidden className="h-0.5 w-3 rounded-full" style={{ background: color }} />}
      <span className="font-semibold text-ink tabular-nums">{value}</span>
      <span className="text-ink-2">{label}</span>
    </p>
  )
}
