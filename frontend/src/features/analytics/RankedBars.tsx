import type { CountDto } from '../../api/types'
import { formatNumber } from '../../lib/format'
import { ChartCard } from './ChartCard'

type Props = {
  title: string
  hint?: string
  items: CountDto[]
  mono?: boolean
}

export function RankedBars({ title, hint, items, mono = false }: Props) {
  const max = Math.max(1, ...items.map((item) => item.requests))

  return (
    <ChartCard title={title} hint={hint} isEmpty={items.length === 0}>
      <ul className="flex flex-col gap-2.5">
        {items.map((item) => (
          <li key={item.label} className="grid grid-cols-[minmax(0,11rem)_1fr_auto] items-center gap-3 text-sm">
            <span className={`truncate text-ink ${mono ? 'font-mono text-xs' : ''}`} title={item.label}>
              {item.label}
            </span>
            <span className="h-2.5">
              <span
                className="block h-full min-w-0.5 rounded-r bg-(--series-1)"
                style={{ width: `${(item.requests / max) * 100}%` }}
              />
            </span>
            <span className="text-ink-2 tabular-nums">{formatNumber(item.requests)}</span>
          </li>
        ))}
      </ul>
    </ChartCard>
  )
}
