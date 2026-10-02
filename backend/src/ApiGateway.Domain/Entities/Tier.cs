namespace ApiGateway.Domain.Entities;

public sealed class Tier : Entity
{
    private Tier() { }

    public Tier(Guid apiId, string name, int requestsPerMinute, int monthlyQuota, decimal creditCostPerRequest)
    {
        ApiId = apiId;
        Update(name, requestsPerMinute, monthlyQuota, creditCostPerRequest);
    }

    public Guid ApiId { get; private set; }
    public string Name { get; private set; } = null!;
    public int RequestsPerMinute { get; private set; }
    public int MonthlyQuota { get; private set; }
    public decimal CreditCostPerRequest { get; private set; }

    public void Update(string name, int requestsPerMinute, int monthlyQuota, decimal creditCostPerRequest)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(requestsPerMinute);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(monthlyQuota);
        ArgumentOutOfRangeException.ThrowIfNegative(creditCostPerRequest);

        Name = name.Trim();
        RequestsPerMinute = requestsPerMinute;
        MonthlyQuota = monthlyQuota;
        CreditCostPerRequest = creditCostPerRequest;
    }
}
