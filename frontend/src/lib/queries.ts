import { useMutation, useQueryClient } from '@tanstack/react-query'

export const LIVE_REFRESH_MS = 5000

export function useApiMutation<TVariables, TResult>(mutationFn: (variables: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn,
    onSuccess: () => queryClient.invalidateQueries(),
  })
}
