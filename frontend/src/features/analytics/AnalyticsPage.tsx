import type { ComponentType } from 'react'
import { PageHeader } from '../../components/Display'
import { QueryView } from '../../components/Feedback'
import { FilterBar } from './AnalyticsFilters'
import { useUsageData, type Audience, type UsageData } from './queries'
import { ConsumersView, CreditsView, OverviewView, PerformanceView, TrafficView } from './views'

type View = {
  title: string
  description: Record<Audience, string>
  component: ComponentType<{ data: UsageData; audience: Audience }>
}

const VIEWS = {
  overview: {
    title: 'Overview',
    description: {
      owner: 'Traffic through the gateway for your APIs. Refreshes every few seconds.',
      consumer: 'Your usage at a glance. Refreshes every few seconds.',
    },
    component: OverviewView,
  },
  traffic: {
    title: 'Traffic',
    description: {
      owner: 'How much is being called, when, and what.',
      consumer: 'What you call, when, and how often.',
    },
    component: TrafficView,
  },
  performance: {
    title: 'Performance',
    description: {
      owner: 'How reliably and how fast the upstream APIs answer.',
      consumer: 'How reliably and how fast the APIs you use answer.',
    },
    component: PerformanceView,
  },
  consumers: {
    title: 'Usage by consumer',
    description: { owner: 'Who is using your APIs, and how their requests end.', consumer: '' },
    component: ConsumersView,
  },
  credits: {
    title: 'Credits',
    description: {
      owner: 'What consumers were charged for successful requests.',
      consumer: 'What your successful requests cost.',
    },
    component: CreditsView,
  },
} satisfies Record<string, View>

export type AnalyticsView = keyof typeof VIEWS

export function AnalyticsPage({ audience, view }: { audience: Audience; view: AnalyticsView }) {
  const usage = useUsageData(audience)
  const { title, description, component: ViewComponent } = VIEWS[view]

  return (
    <>
      <PageHeader title={title} description={description[audience]} />
      <FilterBar showApi={audience === 'owner'} />
      <QueryView query={usage}>{(data) => <ViewComponent data={data} audience={audience} />}</QueryView>
    </>
  )
}

export function ApiUsage({ apiId }: { apiId: string }) {
  const usage = useUsageData('owner', apiId)

  return (
    <>
      <FilterBar />
      <QueryView query={usage}>{(data) => <OverviewView data={data} audience="owner" />}</QueryView>
    </>
  )
}
