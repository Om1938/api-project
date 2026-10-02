using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiGateway.IntegrationTests.Support;

public sealed record ReceivedRequest(string Method, string Path, string Query, IReadOnlyDictionary<string, string> Headers, string Body);

public sealed class UpstreamStub : IAsyncDisposable
{
    private readonly WebApplication _app;

    private UpstreamStub(WebApplication app) => _app = app;

    public string BaseUrl { get; private set; } = null!;
    public ConcurrentQueue<ReceivedRequest> ApiRequests { get; } = new();
    public ConcurrentQueue<ReceivedRequest> WebhookRequests { get; } = new();

    public static async Task<UpstreamStub> StartAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var stub = new UpstreamStub(builder.Build());
        stub.MapRoutes();
        await stub._app.StartAsync();
        stub.BaseUrl = stub._app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();

        return stub;
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    private void MapRoutes()
    {
        _app.Map("/api/status/{code:int}", async (HttpContext http, int code) =>
        {
            ApiRequests.Enqueue(await CaptureAsync(http.Request));
            return Results.StatusCode(code);
        });

        _app.Map("/api/abort", (HttpContext http) => http.Abort());

        _app.Map("/api/{**rest}", async (HttpContext http) =>
        {
            var received = await CaptureAsync(http.Request);
            ApiRequests.Enqueue(received);
            return Results.Json(new { received.Method, received.Path, received.Query });
        });

        _app.MapPost("/hooks/{name}", async (HttpContext http) =>
        {
            WebhookRequests.Enqueue(await CaptureAsync(http.Request));
            return Results.Ok();
        });

        _app.MapPost("/failing-hooks/{name}", async (HttpContext http) =>
        {
            WebhookRequests.Enqueue(await CaptureAsync(http.Request));
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        });
    }

    private static async Task<ReceivedRequest> CaptureAsync(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body);
        return new ReceivedRequest(
            request.Method,
            request.Path,
            request.QueryString.Value ?? string.Empty,
            request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase),
            await reader.ReadToEndAsync());
    }
}
