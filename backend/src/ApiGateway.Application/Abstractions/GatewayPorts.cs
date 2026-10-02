using ApiGateway.Application.Common;
using ApiGateway.Application.Gateway;

namespace ApiGateway.Application.Abstractions;

public interface IFixedWindowCounter
{
    Task<long> IncrementAsync(string key, TimeSpan timeToLive, CancellationToken cancellationToken);

    Task<long> IncrementAsync(
        string key,
        TimeSpan timeToLive,
        Func<CancellationToken, Task<long>> seed,
        CancellationToken cancellationToken);
}

public interface IKeyContextResolver
{
    Task<KeyContext?> ResolveAsync(string keyHash, CancellationToken cancellationToken);
}

public interface IKeyContextInvalidator
{
    Task InvalidateAsync(IEnumerable<string> keyHashes, CancellationToken cancellationToken);
}

public interface ICreditLedger
{
    Task<decimal> GetBalanceAsync(Guid consumerId, CancellationToken cancellationToken);

    Task<bool> TryChargeAsync(Guid consumerId, decimal amount, CancellationToken cancellationToken);

    Task DepositAsync(Guid consumerId, decimal amount, CancellationToken cancellationToken);
}

public interface IUsageSink
{
    void Record(UsageEvent usageEvent);
}

public interface IQuotaUsageReader
{
    Task<long> CountAsync(Guid apiKeyId, FixedWindow window, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, long>> CountByKeyAsync(
        IReadOnlyCollection<Guid> apiKeyIds,
        FixedWindow window,
        CancellationToken cancellationToken);
}

public interface IQuotaAlertSink
{
    Task PublishAsync(QuotaAlert alert, CancellationToken cancellationToken);
}
