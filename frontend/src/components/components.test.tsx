import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { formatCompact } from '../lib/format'
import { DataTable } from './DataTable'
import { Meter } from './Display'

describe('DataTable', () => {
  const columns = [
    { header: 'Name', cell: (row: { name: string; count: number }) => row.name },
    { header: 'Count', numeric: true, cell: (row: { name: string; count: number }) => row.count },
  ]

  it('renders a header and one row per item', () => {
    render(<DataTable columns={columns} rows={[{ name: 'Free', count: 3 }, { name: 'Pro', count: 9 }]} rowKey={(row) => row.name} empty="nothing" />)

    expect(screen.getAllByRole('columnheader').map((cell) => cell.textContent)).toEqual(['Name', 'Count'])
    expect(screen.getAllByRole('row')).toHaveLength(3)
    expect(screen.getByText('Pro')).toBeInTheDocument()
  })

  it('renders the empty state instead of an empty table', () => {
    render(<DataTable columns={columns} rows={[]} rowKey={(row) => row.name} empty={<p>No tiers yet</p>} />)

    expect(screen.getByText('No tiers yet')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })
})

describe('Meter', () => {
  it('exposes the ratio to assistive technology and caps it at the limit', () => {
    render(<Meter value={150} max={100} label="Quota used" />)

    const meter = screen.getByRole('meter', { name: 'Quota used' })
    expect(meter).toHaveAttribute('aria-valuenow', '100')
    expect(meter).toHaveAttribute('aria-valuemax', '100')
  })
})

describe('formatCompact', () => {
  it('keeps small numbers exact and compacts large ones', () => {
    expect(formatCompact(1284)).toBe('1,284')
    expect(formatCompact(12_900)).toBe('12.9K')
    expect(formatCompact(4_200_000)).toBe('4.2M')
  })
})
