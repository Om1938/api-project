using ApiGateway.Application.Gateway;
using ApiGateway.Domain.Entities;
using ApiGateway.Infrastructure.Persistence;
using ApiGateway.Infrastructure.Persistence.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiGateway.Infrastructure.Usage;

internal sealed class UsageWriterService(
    UsageChannel channel,
    IServiceScopeFactory scopeFactory,
    ILogger<UsageWriterService> logger) : BackgroundService
{
    private const int MaxBatchSize = 500;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<UsageEvent>(MaxBatchSize);

        try
        {
            while (await channel.Reader.WaitToReadAsync(stoppingToken))
            {
                await DrainAsync(batch);
            }
        }
        catch (OperationCanceledException)
        {
            // shutting down: flush whatever is still buffered
        }

        channel.Complete();
        await DrainAsync(batch);
    }

    private async Task DrainAsync(List<UsageEvent> batch)
    {
        while (channel.Reader.TryRead(out var usageEvent))
        {
            batch.Add(usageEvent);
            if (batch.Count == MaxBatchSize)
            {
                await WriteAsync(batch);
            }
        }

        await WriteAsync(batch);
    }

    private async Task WriteAsync(List<UsageEvent> batch)
    {
        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.UsageRecords.AddRange(batch.Select(ToRecord));
            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to persist {Count} usage records; they are lost", batch.Count);
        }
        finally
        {
            batch.Clear();
        }
    }

    private static UsageRecord ToRecord(UsageEvent usageEvent) => new()
    {
        ApiKeyId = usageEvent.ApiKeyId,
        ApiId = usageEvent.ApiId,
        ConsumerId = usageEvent.ConsumerId,
        Timestamp = usageEvent.Timestamp.UtcDateTime,
        Method = usageEvent.Method,
        Path = usageEvent.Path.Length <= UsageRecordLimits.PathMaxLength
            ? usageEvent.Path
            : usageEvent.Path[..UsageRecordLimits.PathMaxLength],
        StatusCode = usageEvent.StatusCode,
        Outcome = usageEvent.Outcome,
        LatencyMs = usageEvent.LatencyMs,
        CreditsCharged = usageEvent.CreditsCharged,
    };
}
