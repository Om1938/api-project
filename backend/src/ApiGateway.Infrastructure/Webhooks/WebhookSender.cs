using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using ApiGateway.Domain.Entities;

namespace ApiGateway.Infrastructure.Webhooks;

public sealed class WebhookOptions
{
    public const string Section = "Webhooks";

    public int MaxAttempts { get; set; } = 3;

    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(2);

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}

internal sealed record WebhookSendResult(int? StatusCode, bool Success, string? Error);

internal interface IWebhookSender
{
    Task<WebhookSendResult> SendAsync(WebhookSubscription subscription, WebhookDelivery delivery, CancellationToken cancellationToken);
}

internal sealed class WebhookSender(HttpClient httpClient) : IWebhookSender
{
    public const string EventHeader = "X-Gateway-Event";
    public const string DeliveryHeader = "X-Gateway-Delivery";
    public const string SignatureHeader = "X-Gateway-Signature";

    public async Task<WebhookSendResult> SendAsync(
        WebhookSubscription subscription,
        WebhookDelivery delivery,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url);
        request.Content = new StringContent(delivery.Payload, Encoding.UTF8);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Add(EventHeader, delivery.EventType);
        request.Headers.Add(DeliveryHeader, delivery.Id.ToString());
        request.Headers.Add(SignatureHeader, Sign(subscription.Secret, delivery.Payload));

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var status = (int)response.StatusCode;
            return new WebhookSendResult(status, response.IsSuccessStatusCode, response.IsSuccessStatusCode ? null : $"HTTP {status}");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return new WebhookSendResult(null, false, exception.Message);
        }
    }

    public static string Sign(string secret, string payload) =>
        "sha256=" + Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload)));
}
