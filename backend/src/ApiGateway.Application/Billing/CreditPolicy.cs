namespace ApiGateway.Application.Billing;

public static class CreditPolicy
{
    public const decimal WelcomeBonus = 100m;

    public const decimal MinTopUp = 1m;
    public const decimal MaxTopUp = 10_000m;
}
