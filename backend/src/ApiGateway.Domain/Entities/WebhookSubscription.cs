using System.Security.Cryptography;
using ApiGateway.Domain.Enums;

namespace ApiGateway.Domain.Entities;

public sealed class WebhookSubscription : Entity
{
    private WebhookSubscription() { }

    public WebhookSubscription(Guid apiId, string url, int thresholdPercent, WebhookEvents events, bool isActive = true)
    {
        ApiId = apiId;
        Secret = "whsec_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(24));
        Update(url, thresholdPercent, events, isActive);
    }

    public Guid ApiId { get; private set; }
    public string Url { get; private set; } = null!;

    public string Secret { get; private set; } = null!;

    public int ThresholdPercent { get; private set; }

    public WebhookEvents Events { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string url, int thresholdPercent, WebhookEvents events, bool isActive)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(thresholdPercent, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(thresholdPercent, 100);

        Url = url.Trim();
        ThresholdPercent = thresholdPercent;
        Events = events;
        IsActive = isActive;
    }
}
