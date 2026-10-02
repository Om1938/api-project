using ApiGateway.Domain.Enums;

namespace ApiGateway.Application.Webhooks;

public static class WebhookEventNames
{
    public const string QuotaThresholdReached = "quota.threshold_reached";
    public const string QuotaExceeded = "quota.exceeded";

    private static readonly IReadOnlyDictionary<string, WebhookEvents> ByName = new Dictionary<string, WebhookEvents>
    {
        [QuotaThresholdReached] = WebhookEvents.QuotaThresholdReached,
        [QuotaExceeded] = WebhookEvents.QuotaExceeded,
    };

    public static IReadOnlyCollection<string> All => (IReadOnlyCollection<string>)ByName.Keys;

    public static bool IsKnown(string name) => ByName.ContainsKey(name);

    public static string NameOf(WebhookEvents single) => ByName.Single(pair => pair.Value == single).Key;

    public static WebhookEvents ToFlags(IEnumerable<string> names) =>
        names.Aggregate(WebhookEvents.None, (flags, name) => flags | ByName[name]);

    public static string[] ToNames(WebhookEvents flags) =>
        ByName.Where(pair => flags.HasFlag(pair.Value)).Select(pair => pair.Key).ToArray();
}
