import { useMemo, useState, type ReactNode } from 'react'
import { InlineSelect } from '../../components/Field'
import { useApis } from '../apis/queries'
import { bucketOptions, DEFAULT_RANGE, RANGE_PRESETS, type RangePreset } from './range'
import { AnalyticsFiltersContext, useAnalyticsFilters, type AnalyticsFilters } from './useAnalyticsFilters'

export function AnalyticsFiltersProvider({ children }: { children: ReactNode }) {
  const [range, setRangeState] = useState<RangePreset>(DEFAULT_RANGE)
  const [bucket, setBucket] = useState<number | null>(null)
  const [apiId, setApiId] = useState('')

  const value = useMemo<AnalyticsFilters>(
    () => ({
      range,
      bucket,
      apiId,
      setBucket,
      setApiId,
      setRange: (next) => {
        setRangeState(next)
        // a resolution that the new range cannot show falls back to automatic
        setBucket((current) => (bucketOptions(next).some((option) => option.minutes === current) ? current : null))
      },
    }),
    [range, bucket, apiId],
  )

  return <AnalyticsFiltersContext.Provider value={value}>{children}</AnalyticsFiltersContext.Provider>
}

function ApiSelect() {
  const { apiId, setApiId } = useAnalyticsFilters()
  const apis = useApis()

  return (
    <InlineSelect aria-label="API" value={apiId} onChange={(event) => setApiId(event.target.value)}>
      <option value="">All APIs</option>
      {apis.data?.map((api) => (
        <option key={api.id} value={api.id}>
          {api.name}
        </option>
      ))}
    </InlineSelect>
  )
}

export function FilterBar({ showApi = false }: { showApi?: boolean }) {
  const { range, bucket, setRange, setBucket } = useAnalyticsFilters()

  return (
    <div className="mb-4 flex flex-wrap items-center gap-2">
      <InlineSelect aria-label="Time range" value={range} onChange={(event) => setRange(event.target.value as RangePreset)}>
        {RANGE_PRESETS.map((preset) => (
          <option key={preset.value} value={preset.value}>
            {preset.label}
          </option>
        ))}
      </InlineSelect>

      <InlineSelect
        aria-label="Resolution"
        value={bucket ?? ''}
        onChange={(event) => setBucket(event.target.value ? Number(event.target.value) : null)}
      >
        <option value="">Resolution: auto</option>
        {bucketOptions(range).map((option) => (
          <option key={option.minutes} value={option.minutes}>
            Per {option.label}
          </option>
        ))}
      </InlineSelect>

      {showApi && <ApiSelect />}
    </div>
  )
}
