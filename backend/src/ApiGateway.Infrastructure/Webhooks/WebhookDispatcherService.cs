using System.Text.Json;
using ApiGateway.Application.Gateway;
using ApiGateway.Application.Webhooks;
using ApiGateway.Domain.Entities;
using ApiGateway.Domain.Enums;
using ApiGateway.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiGateway.Infrastructure.Webhooks;

internal sealed class WebhookDispatcherService(
    QuotaAlertChannel channel,
    IServiceScopeFactory scopeFactory,
    IOptions<WebhookOptions> options,
    TimeProvider timeProvider,
    ILogger<WebhookDispatcherService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions PayloadJson = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var alert in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await DispatchAsync(alert, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to dispatch {Event} for key {ApiKeyId}", alert.Event, alert.ApiKeyId);
            }
        }
    }

    private async Task DispatchAsync(QuotaAlert alert, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IWebhookSender>();

        var subscriptions = await db.WebhookSubscriptions
            .AsNoTracking()
            .Where(w => w.ApiId == alert.ApiId && w.IsActive && w.Events.HasFlag(alert.Event))
            .ToListAsync(cancellationToken);

        foreach (var subscription in subscriptions.Where(s => Wants(s, alert)))
        {
            var eventType = WebhookEventNames.NameOf(alert.Event);
            var delivery = new WebhookDelivery(
                subscription.Id, alert.ApiKeyId, eventType, deliveryId => BuildPayload(deliveryId, eventType, alert));

            await SendWithRetriesAsync(sender, subscription, delivery, cancellationToken);

            db.WebhookDeliveries.Add(delivery);
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }

    private static bool Wants(WebhookSubscription subscription, QuotaAlert alert) =>
        alert.Event != WebhookEvents.QuotaThresholdReached || subscription.ThresholdPercent == alert.ThresholdPercent;

    private async Task SendWithRetriesAsync(
        IWebhookSender sender,
        WebhookSubscription subscription,
        WebhookDelivery delivery,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var delay = settings.RetryBaseDelay;

        for (var attempt = 1; attempt <= settings.MaxAttempts; attempt++)
        {
            var result = await sender.SendAsync(subscription, delivery, cancellationToken);
            delivery.RecordAttempt(result.StatusCode, result.Success, result.Error);

            if (result.Success || attempt == settings.MaxAttempts)
            {
                return;
            }

            await Task.Delay(delay, timeProvider, cancellationToken);
            delay *= 2;
        }
    }

    private static string BuildPayload(Guid deliveryId, string eventType, QuotaAlert alert) =>
        JsonSerializer.Serialize(
            new
            {
                Id = deliveryId,
                Event = eventType,
                alert.OccurredAt,
                Data = new
                {
                    alert.ApiId,
                    alert.ApiKeyId,
                    alert.ConsumerId,
                    alert.Period,
                    alert.Used,
                    alert.Limit,
                    alert.ThresholdPercent,
                },
            },
            PayloadJson);
}
