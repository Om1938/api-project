import { createContext, useContext } from 'react'
import type { RangePreset } from './range'

export type AnalyticsFilters = {
  range: RangePreset
  bucket: number | null // minutes; null lets the backend choose
  apiId: string
  setRange: (range: RangePreset) => void
  setBucket: (bucket: number | null) => void
  setApiId: (apiId: string) => void
}

export const AnalyticsFiltersContext = createContext<AnalyticsFilters | null>(null)

export function useAnalyticsFilters(): AnalyticsFilters {
  const value = useContext(AnalyticsFiltersContext)
  if (!value) {
    throw new Error('useAnalyticsFilters must be used inside <AnalyticsFiltersProvider>.')
  }
  return value
}
