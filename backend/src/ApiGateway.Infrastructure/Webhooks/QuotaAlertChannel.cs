using System.Threading.Channels;
using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Common;
using ApiGateway.Application.Gateway;
using StackExchange.Redis;

namespace ApiGateway.Infrastructure.Webhooks;

internal sealed class QuotaAlertChannel(IConnectionMultiplexer redis) : IQuotaAlertSink
{
    private static readonly TimeSpan MarkerTtl = TimeSpan.FromDays(40);

    private readonly Channel<QuotaAlert> _channel = Channel.CreateUnbounded<QuotaAlert>(
        new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<QuotaAlert> Reader => _channel.Reader;

    public async Task PublishAsync(QuotaAlert alert, CancellationToken cancellationToken)
    {
        var marker = StoreKeys.QuotaAlertSent(alert.ApiKeyId, alert.Period, alert.Event, alert.ThresholdPercent);
        var isFirst = await redis.GetDatabase().StringSetAsync(marker, "1", MarkerTtl, When.NotExists);

        if (isFirst)
        {
            _channel.Writer.TryWrite(alert);
        }
    }
}
