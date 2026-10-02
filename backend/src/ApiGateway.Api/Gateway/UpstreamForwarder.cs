using System.Net;
using Yarp.ReverseProxy.Forwarder;

namespace ApiGateway.Api.Gateway;

public interface IUpstreamForwarder
{
    Task ForwardAsync(HttpContext httpContext, string targetBaseUrl, PathString upstreamPath);
}

internal sealed class YarpUpstreamForwarder(IHttpForwarder forwarder, ILogger<YarpUpstreamForwarder> logger)
    : IUpstreamForwarder, IDisposable
{
    private static readonly ForwarderRequestConfig RequestConfig = new() { ActivityTimeout = TimeSpan.FromSeconds(30) };
    private static readonly GatewayTransformer Transformer = new();

    private readonly HttpMessageInvoker _client = new(new SocketsHttpHandler
    {
        UseProxy = false,
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.None,
        UseCookies = false,
        ConnectTimeout = TimeSpan.FromSeconds(10),
    });

    public async Task ForwardAsync(HttpContext httpContext, string targetBaseUrl, PathString upstreamPath)
    {
        // YARP appends Request.Path to the destination
        httpContext.Request.PathBase = PathString.Empty;
        httpContext.Request.Path = upstreamPath;

        var error = await forwarder.SendAsync(httpContext, targetBaseUrl, _client, RequestConfig, Transformer);
        if (error != ForwarderError.None)
        {
            var exception = httpContext.Features.Get<IForwarderErrorFeature>()?.Exception;
            logger.LogWarning(exception, "Forwarding to {Target} failed: {Error}", targetBaseUrl, error);
        }
    }

    public void Dispose() => _client.Dispose();

    private sealed class GatewayTransformer : HttpTransformer
    {
        public override async ValueTask TransformRequestAsync(
            HttpContext httpContext,
            HttpRequestMessage proxyRequest,
            string destinationPrefix,
            CancellationToken cancellationToken)
        {
            await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);

            proxyRequest.Headers.Remove(GatewayHeaders.ApiKey);
            proxyRequest.Headers.Host = null; // use the upstream's host, not ours
        }
    }
}
