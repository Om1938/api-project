using System.Threading.Channels;
using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Gateway;
using Microsoft.Extensions.Logging;

namespace ApiGateway.Infrastructure.Usage;

internal sealed class UsageChannel(ILogger<UsageChannel> logger) : IUsageSink
{
    private const int Capacity = 50_000;

    private readonly Channel<UsageEvent> _channel = Channel.CreateBounded<UsageEvent>(
        new BoundedChannelOptions(Capacity) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });

    public ChannelReader<UsageEvent> Reader => _channel.Reader;

    public void Record(UsageEvent usageEvent)
    {
        if (!_channel.Writer.TryWrite(usageEvent))
        {
            logger.LogWarning("Usage buffer is full; dropping usage event for key {ApiKeyId}", usageEvent.ApiKeyId);
        }
    }

    public void Complete() => _channel.Writer.TryComplete();
}
