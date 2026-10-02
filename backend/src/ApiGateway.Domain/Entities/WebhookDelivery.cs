namespace ApiGateway.Domain.Entities;

public sealed class WebhookDelivery : Entity
{
    private WebhookDelivery() { }

    public WebhookDelivery(Guid subscriptionId, Guid apiKeyId, string eventType, Func<Guid, string> buildPayload)
    {
        SubscriptionId = subscriptionId;
        ApiKeyId = apiKeyId;
        EventType = eventType;
        Payload = buildPayload(Id);
    }

    public Guid SubscriptionId { get; private set; }
    public Guid ApiKeyId { get; private set; }
    public string EventType { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public int? ResponseStatus { get; private set; }
    public bool Success { get; private set; }
    public int Attempts { get; private set; }
    public string? Error { get; private set; }

    public void RecordAttempt(int? responseStatus, bool success, string? error)
    {
        Attempts++;
        ResponseStatus = responseStatus;
        Success = success;
        Error = success ? null : error;
    }
}
