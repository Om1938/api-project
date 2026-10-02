import { useState } from 'react'
import type { DayOfWeek, HeatmapCellDto } from '../../api/types'
import { formatNumber } from '../../lib/format'
import { ChartCard } from './ChartCard'

const DAYS: DayOfWeek[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']
const HOURS = Array.from({ length: 24 }, (_, hour) => hour)
const LEGEND_STEPS = [0.1, 0.3, 0.55, 0.8, 1]

const EMPTY_CELL = 'color-mix(in oklab, var(--line) 55%, var(--surface))'

const hourLabel = (hour: number) => `${String(hour).padStart(2, '0')}:00`

// one hue fading into the surface, so "less" recedes in both light and dark mode.
// intensity is on a square-root scale: one very busy hour must not wash out the rest.
const shade = (intensity: number) => `color-mix(in oklab, var(--series-1) ${Math.round(12 + intensity * 88)}%, var(--surface))`

export function TrafficHeatmap({ cells }: { cells: HeatmapCellDto[] }) {
  const [hovered, setHovered] = useState<{ day: DayOfWeek; hour: number } | null>(null)
  const counts = new Map(cells.map((cell) => [`${cell.dayOfWeek}-${cell.hour}`, cell.requests]))
  const count = (day: DayOfWeek, hour: number) => counts.get(`${day}-${hour}`) ?? 0
  const max = Math.max(1, ...cells.map((cell) => cell.requests))

  return (
    <ChartCard
      title="When traffic arrives"
      hint="Requests by day of week and hour of day (UTC). Stronger colour means busier."
      isEmpty={cells.length === 0}
      className="lg:col-span-2"
      table={{
        rows: [...cells].sort((a, b) => b.requests - a.requests),
        rowKey: (cell) => `${cell.dayOfWeek}-${cell.hour}`,
        columns: [
          { header: 'Day', cell: (cell) => cell.dayOfWeek },
          { header: 'Hour (UTC)', cell: (cell) => hourLabel(cell.hour) },
          { header: 'Requests', numeric: true, cell: (cell) => formatNumber(cell.requests) },
        ],
      }}
    >
      <div
        className="grid gap-0.5 text-xs text-muted"
        style={{ gridTemplateColumns: '2.25rem repeat(24, minmax(0, 1fr))' }}
        onMouseLeave={() => setHovered(null)}
      >
        {DAYS.map((day) => (
          <div key={day} className="contents">
            <span className="flex items-center">{day.slice(0, 3)}</span>
            {HOURS.map((hour) => {
              const requests = count(day, hour)
              const isHovered = hovered?.day === day && hovered.hour === hour
              return (
                <span
                  key={hour}
                  role="img"
                  aria-label={`${day} ${hourLabel(hour)}: ${requests} requests`}
                  onMouseEnter={() => setHovered({ day, hour })}
                  className={`h-6 rounded-xs ${isHovered ? 'outline-2 outline-ink' : ''}`}
                  style={{ background: requests === 0 ? EMPTY_CELL : shade(Math.sqrt(requests / max)) }}
                />
              )
            })}
          </div>
        ))}

        <span />
        {HOURS.map((hour) => (
          <span key={hour} className="pt-1 text-center tabular-nums">
            {hour % 3 === 0 ? String(hour).padStart(2, '0') : ''}
          </span>
        ))}
      </div>

      <div className="mt-3 flex flex-wrap items-center justify-between gap-3 text-xs text-ink-2">
        <p aria-live="polite">
          {hovered ? (
            <>
              <span className="font-semibold text-ink tabular-nums">{formatNumber(count(hovered.day, hovered.hour))}</span> requests on{' '}
              {hovered.day}, {hourLabel(hovered.hour)}–{hourLabel((hovered.hour + 1) % 24)}
            </>
          ) : (
            'Hover a cell for its count.'
          )}
        </p>
        <p className="flex items-center gap-1.5">
          Fewer
          {LEGEND_STEPS.map((step) => (
            <span key={step} aria-hidden className="size-3 rounded-xs" style={{ background: shade(step) }} />
          ))}
          More
        </p>
      </div>
    </ChartCard>
  )
}
