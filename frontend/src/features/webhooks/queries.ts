import { useQuery } from '@tanstack/react-query'
import { api } from '../../api/client'
import type { WebhookDeliveryDto, WebhookDto, WebhookRequest } from '../../api/types'
import { LIVE_REFRESH_MS, useApiMutation } from '../../lib/queries'
import { apiPath } from '../apis/queries'

export const WEBHOOK_EVENTS = [
  { name: 'quota.threshold_reached', label: 'Quota threshold reached' },
  { name: 'quota.exceeded', label: 'Quota exceeded' },
] as const

export const eventLabel = (name: string) => WEBHOOK_EVENTS.find((event) => event.name === name)?.label ?? name

export const useWebhooks = (apiId: string) =>
  useQuery({ queryKey: ['apis', apiId, 'webhooks'], queryFn: () => api.get<WebhookDto[]>(apiPath(apiId, '/webhooks')) })

export const useDeliveries = (apiId: string) =>
  useQuery({
    queryKey: ['apis', apiId, 'webhook-deliveries'],
    queryFn: () => api.get<WebhookDeliveryDto[]>(apiPath(apiId, '/webhooks/deliveries')),
    refetchInterval: LIVE_REFRESH_MS,
  })

export const useSaveWebhook = (apiId: string, webhookId?: string) =>
  useApiMutation((request: WebhookRequest) =>
    webhookId
      ? api.put<WebhookDto>(apiPath(apiId, `/webhooks/${webhookId}`), request)
      : api.post<WebhookDto>(apiPath(apiId, '/webhooks'), request),
  )

export const useDeleteWebhook = (apiId: string) =>
  useApiMutation((webhookId: string) => api.delete(apiPath(apiId, `/webhooks/${webhookId}`)))
