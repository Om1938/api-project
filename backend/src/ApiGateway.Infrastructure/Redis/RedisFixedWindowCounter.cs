using ApiGateway.Application.Abstractions;
using StackExchange.Redis;

namespace ApiGateway.Infrastructure.Redis;

internal sealed class RedisFixedWindowCounter(IConnectionMultiplexer redis) : IFixedWindowCounter
{
    private const long KeyMissing = -1;

    private const string IncrementScript = """
        local count = redis.call('INCR', KEYS[1])
        if count == 1 then
            redis.call('PEXPIRE', KEYS[1], ARGV[1])
        end
        return count
        """;

    private const string IncrementIfExistsScript = """
        if redis.call('EXISTS', KEYS[1]) == 0 then
            return -1
        end
        return redis.call('INCR', KEYS[1])
        """;

    private const string SeedAndIncrementScript = """
        redis.call('SET', KEYS[1], ARGV[1], 'NX', 'PX', ARGV[2])
        return redis.call('INCR', KEYS[1])
        """;

    public async Task<long> IncrementAsync(string key, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        var result = await Database.ScriptEvaluateAsync(IncrementScript, [key], [Milliseconds(timeToLive)]);
        return (long)result;
    }

    public async Task<long> IncrementAsync(
        string key,
        TimeSpan timeToLive,
        Func<CancellationToken, Task<long>> seed,
        CancellationToken cancellationToken)
    {
        var count = (long)await Database.ScriptEvaluateAsync(IncrementIfExistsScript, [key]);
        if (count != KeyMissing)
        {
            return count;
        }

        var seedValue = await seed(cancellationToken);
        var result = await Database.ScriptEvaluateAsync(SeedAndIncrementScript, [key], [seedValue, Milliseconds(timeToLive)]);
        return (long)result;
    }

    private IDatabase Database => redis.GetDatabase();

    private static long Milliseconds(TimeSpan timeToLive) => Math.Max(1, (long)timeToLive.TotalMilliseconds);
}
