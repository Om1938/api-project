import type { components } from './schema'

// aliases for the generated schema (npm run gen:api)
type Schemas = components['schemas']

export type UserRole = Schemas['UserRole']
export type UserDto = Schemas['UserDto']
export type AuthResponse = Schemas['AuthResponse']
export type LoginRequest = Schemas['LoginRequest']
export type RegisterRequest = Schemas['RegisterRequest']

export type ApiDto = Schemas['ApiDto']
export type CreateApiRequest = Schemas['CreateApiRequest']
export type UpdateApiRequest = Schemas['UpdateApiRequest']

export type TierDto = Schemas['TierDto']
export type TierRequest = Schemas['TierRequest']

export type ApiKeyDto = Schemas['ApiKeyDto']
export type CreateApiKeyRequest = Schemas['CreateApiKeyRequest']
export type CreatedApiKeyDto = Schemas['CreatedApiKeyDto']

export type WebhookDto = Schemas['WebhookDto']
export type WebhookRequest = Schemas['WebhookRequest']
export type WebhookDeliveryDto = Schemas['WebhookDeliveryDto']

export type ConsumerDto = Schemas['ConsumerDto']
export type MyKeyDto = Schemas['MyKeyDto']

export type UsageSummaryDto = Schemas['UsageSummaryDto']
export type UsagePointDto = Schemas['UsagePointDto']
export type UsageReportDto = Schemas['UsageReportDto']
export type UsageBreakdownDto = Schemas['UsageBreakdownDto']
export type CountDto = Schemas['CountDto']
export type HeatmapCellDto = Schemas['HeatmapCellDto']
export type DayOfWeek = Schemas['DayOfWeek']
export type ConsumerUsageDto = Schemas['ConsumerUsageDto']
export type OwnerAnalyticsDto = Schemas['OwnerAnalyticsDto']

export type CreditAccountDto = Schemas['CreditAccountDto']
export type CreditTransactionDto = Schemas['CreditTransactionDto']
export type TopUpRequest = Schemas['TopUpRequest']
