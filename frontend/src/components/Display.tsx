import { useState, type ReactNode } from 'react'
import { Button } from './Button'

export function PageHeader({ title, description, actions }: { title: string; description?: ReactNode; actions?: ReactNode }) {
  return (
    <header className="mb-6 flex flex-wrap items-start justify-between gap-4">
      <div>
        <h1 className="text-2xl font-semibold text-ink">{title}</h1>
        {description && <p className="mt-1 text-sm text-ink-2">{description}</p>}
      </div>
      {actions && <div className="flex gap-2">{actions}</div>}
    </header>
  )
}

export function Card({ title, actions, children }: { title?: string; actions?: ReactNode; children: ReactNode }) {
  return (
    <section className="rounded-lg border border-line bg-surface p-5">
      {(title || actions) && (
        <div className="mb-4 flex items-center justify-between gap-4">
          {title && <h2 className="text-sm font-semibold text-ink">{title}</h2>}
          {actions}
        </div>
      )}
      {children}
    </section>
  )
}

export function Section({ title, actions, children }: { title: string; actions?: ReactNode; children: ReactNode }) {
  return (
    <section className="mt-8">
      <div className="mb-3 flex items-center justify-between gap-4">
        <h2 className="text-base font-semibold text-ink">{title}</h2>
        {actions}
      </div>
      {children}
    </section>
  )
}

export function StatCard({ label, value, note }: { label: string; value: string; note?: string }) {
  return (
    <div className="rounded-lg border border-line bg-surface px-4 py-3">
      <p className="text-xs text-ink-2">{label}</p>
      <p className="mt-1 text-2xl font-semibold text-ink">{value}</p>
      {note && <p className="mt-0.5 text-xs text-ink-2">{note}</p>}
    </div>
  )
}

type Tone = 'good' | 'critical' | 'neutral'

const toneDot: Record<Tone, string> = {
  good: 'bg-good',
  critical: 'bg-critical',
  neutral: 'bg-muted',
}

export function Badge({ tone, children }: { tone: Tone; children: ReactNode }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-full border border-line px-2 py-0.5 text-xs text-ink">
      <span aria-hidden className={`size-2 rounded-full ${toneDot[tone]}`} />
      {children}
    </span>
  )
}

export function Code({ children }: { children: ReactNode }) {
  return <code className="rounded bg-plane px-1.5 py-0.5 font-mono text-xs break-all text-ink">{children}</code>
}

export function CopyButton({ text, label = 'Copy' }: { text: string; label?: string }) {
  const [copied, setCopied] = useState(false)

  const copy = async () => {
    await navigator.clipboard.writeText(text)
    setCopied(true)
    setTimeout(() => setCopied(false), 1500)
  }

  return (
    <Button size="sm" onClick={copy}>
      {copied ? 'Copied' : label}
    </Button>
  )
}

export function Meter({ value, max, label }: { value: number; max: number; label: string }) {
  const ratio = max > 0 ? Math.min(1, value / max) : 0
  const color = ratio >= 1 ? 'var(--critical)' : ratio >= 0.8 ? 'var(--warning)' : 'var(--accent)'

  return (
    <div
      role="meter"
      aria-label={label}
      aria-valuemin={0}
      aria-valuemax={max}
      aria-valuenow={Math.min(value, max)}
      className="h-2 w-full min-w-24 overflow-hidden rounded-full"
      style={{ background: `color-mix(in srgb, ${color} 18%, var(--surface))` }}
    >
      <div className="h-full rounded-full" style={{ width: `${ratio * 100}%`, background: color }} />
    </div>
  )
}
