import type { ReactNode } from 'react'

export type Column<T> = {
  header: string
  cell: (row: T) => ReactNode
  numeric?: boolean
}

type Props<T> = {
  columns: Column<T>[]
  rows: T[]
  rowKey: (row: T) => string
  empty: ReactNode
}

export function DataTable<T>({ columns, rows, rowKey, empty }: Props<T>) {
  if (rows.length === 0) {
    return <>{empty}</>
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-line bg-surface">
      <table className="w-full text-left text-sm">
        <thead>
          <tr className="border-b border-line text-xs text-ink-2">
            {columns.map((column) => (
              <th key={column.header} scope="col" className={`px-4 py-2.5 font-medium ${column.numeric ? 'text-right' : ''}`}>
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={rowKey(row)} className="border-b border-line last:border-0">
              {columns.map((column) => (
                <td key={column.header} className={`px-4 py-2.5 align-middle text-ink ${column.numeric ? 'text-right tabular-nums' : ''}`}>
                  {column.cell(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
