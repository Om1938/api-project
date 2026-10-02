namespace ApiGateway.Domain.Enums;

public enum UserRole
{
    Owner = 1,
    Consumer = 2,
}

public enum ApiKeyStatus
{
    Active = 1,
    Revoked = 2,
}

public enum RequestOutcome
{
    Success = 1,

    Failed = 2,

    RateLimited = 3,

    QuotaExceeded = 4,

    InsufficientCredits = 5,
}

public enum CreditTransactionType
{
    TopUp = 1,
}

[Flags]
public enum WebhookEvents
{
    None = 0,
    QuotaThresholdReached = 1,
    QuotaExceeded = 2,
}
