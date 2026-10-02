using ApiGateway.Application.Common;
using ApiGateway.Domain.Entities;
using FluentValidation;

namespace ApiGateway.Application.Webhooks;

public sealed record WebhookRequest(string Url, int ThresholdPercent, string[] Events, bool IsActive);

public sealed record WebhookDto(
    Guid Id,
    Guid ApiId,
    string Url,
    string Secret,
    int ThresholdPercent,
    string[] Events,
    bool IsActive,
    DateTime CreatedAt)
{
    public static WebhookDto From(WebhookSubscription subscription) => new(
        subscription.Id,
        subscription.ApiId,
        subscription.Url,
        subscription.Secret,
        subscription.ThresholdPercent,
        WebhookEventNames.ToNames(subscription.Events),
        subscription.IsActive,
        subscription.CreatedAt);
}

public sealed record WebhookDeliveryDto(
    Guid Id,
    Guid SubscriptionId,
    string EventType,
    string Url,
    int? ResponseStatus,
    bool Success,
    int Attempts,
    string? Error,
    string Payload,
    DateTime CreatedAt);

public sealed class WebhookRequestValidator : AbstractValidator<WebhookRequest>
{
    public WebhookRequestValidator()
    {
        RuleFor(x => x.Url).HttpUrl();
        RuleFor(x => x.ThresholdPercent).InclusiveBetween(1, 100);
        RuleFor(x => x.Events).NotEmpty();
        RuleForEach(x => x.Events)
            .Must(WebhookEventNames.IsKnown)
            .WithMessage($"Unknown event. Supported events: {string.Join(", ", WebhookEventNames.All)}.");
    }
}
