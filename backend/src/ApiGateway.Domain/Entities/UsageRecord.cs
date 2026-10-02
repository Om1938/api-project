using ApiGateway.Domain.Enums;

namespace ApiGateway.Domain.Entities;

public sealed class UsageRecord
{
    public long Id { get; init; }
    public Guid ApiKeyId { get; init; }
    public Guid ApiId { get; init; }
    public Guid ConsumerId { get; init; }
    public DateTime Timestamp { get; init; }
    public string Method { get; init; } = null!;
    public string Path { get; init; } = null!;
    public int StatusCode { get; init; }
    public RequestOutcome Outcome { get; init; }
    public int LatencyMs { get; init; }
    public decimal CreditsCharged { get; init; }
}
