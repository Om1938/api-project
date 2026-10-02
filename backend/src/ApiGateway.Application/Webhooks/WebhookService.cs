using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Common;
using ApiGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Application.Webhooks;

public sealed class WebhookService(IAppDbContext db, OwnedApiGuard guard, KeyContextRefresher keyContexts)
{
    private const int MaxDeliveriesListed = 50;

    public async Task<Result<IReadOnlyList<WebhookDto>>> ListAsync(Guid apiId, CancellationToken cancellationToken)
    {
        var api = await guard.FindAsync(apiId, cancellationToken);
        if (!api.IsSuccess)
        {
            return api.Error!;
        }

        var subscriptions = await db.WebhookSubscriptions
            .AsNoTracking()
            .Where(w => w.ApiId == apiId)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(cancellationToken);

        return subscriptions.Select(WebhookDto.From).ToList();
    }

    public async Task<Result<WebhookDto>> CreateAsync(Guid apiId, WebhookRequest request, CancellationToken cancellationToken)
    {
        var api = await guard.FindAsync(apiId, cancellationToken);
        if (!api.IsSuccess)
        {
            return api.Error!;
        }

        var subscription = new WebhookSubscription(
            apiId, request.Url, request.ThresholdPercent, WebhookEventNames.ToFlags(request.Events), request.IsActive);

        db.WebhookSubscriptions.Add(subscription);
        await db.SaveChangesAsync(cancellationToken);
        await keyContexts.ForApiAsync(apiId, cancellationToken);

        return WebhookDto.From(subscription);
    }

    public async Task<Result<WebhookDto>> UpdateAsync(Guid apiId, Guid webhookId, WebhookRequest request, CancellationToken cancellationToken)
    {
        var subscription = await FindAsync(apiId, webhookId, cancellationToken);
        if (!subscription.IsSuccess)
        {
            return subscription.Error!;
        }

        subscription.Value.Update(
            request.Url, request.ThresholdPercent, WebhookEventNames.ToFlags(request.Events), request.IsActive);
        await db.SaveChangesAsync(cancellationToken);
        await keyContexts.ForApiAsync(apiId, cancellationToken);

        return WebhookDto.From(subscription.Value);
    }

    public async Task<Result> DeleteAsync(Guid apiId, Guid webhookId, CancellationToken cancellationToken)
    {
        var subscription = await FindAsync(apiId, webhookId, cancellationToken);
        if (!subscription.IsSuccess)
        {
            return subscription.Error!;
        }

        db.WebhookSubscriptions.Remove(subscription.Value);
        await db.SaveChangesAsync(cancellationToken);
        await keyContexts.ForApiAsync(apiId, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<WebhookDeliveryDto>>> ListDeliveriesAsync(Guid apiId, CancellationToken cancellationToken)
    {
        var api = await guard.FindAsync(apiId, cancellationToken);
        if (!api.IsSuccess)
        {
            return api.Error!;
        }

        var deliveries =
            from delivery in db.WebhookDeliveries.AsNoTracking()
            join subscription in db.WebhookSubscriptions on delivery.SubscriptionId equals subscription.Id
            where subscription.ApiId == apiId
            orderby delivery.CreatedAt descending
            select new WebhookDeliveryDto(
                delivery.Id,
                subscription.Id,
                delivery.EventType,
                subscription.Url,
                delivery.ResponseStatus,
                delivery.Success,
                delivery.Attempts,
                delivery.Error,
                delivery.Payload,
                delivery.CreatedAt);

        return await deliveries.Take(MaxDeliveriesListed).ToListAsync(cancellationToken);
    }

    private async Task<Result<WebhookSubscription>> FindAsync(Guid apiId, Guid webhookId, CancellationToken cancellationToken)
    {
        var api = await guard.FindAsync(apiId, cancellationToken);
        if (!api.IsSuccess)
        {
            return api.Error!;
        }

        var subscription = await db.WebhookSubscriptions
            .FirstOrDefaultAsync(w => w.Id == webhookId && w.ApiId == apiId, cancellationToken);
        if (subscription is null)
        {
            return Error.NotFound("Webhook");
        }

        return subscription;
    }
}
