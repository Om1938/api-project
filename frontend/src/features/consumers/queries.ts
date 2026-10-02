import { useQuery } from '@tanstack/react-query'
import { api } from '../../api/client'
import type { ConsumerDto } from '../../api/types'

export const useConsumers = () =>
  useQuery({ queryKey: ['consumers'], queryFn: () => api.get<ConsumerDto[]>('/api/consumers') })
