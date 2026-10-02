export const API_KEY_HEADER = 'X-API-Key'

export const gatewayUrl = (slug: string, path = '') => `${window.location.origin}/gw/${slug}${path}`

export const curlExample = (slug: string, key: string, path = '/get') =>
  `curl -i "${gatewayUrl(slug, path)}" -H "${API_KEY_HEADER}: ${key}"`
