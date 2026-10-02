using System.Globalization;
using ApiGateway.Api.Infrastructure;
using ApiGateway.Application.Gateway;

namespace ApiGateway.Api.Gateway;

public sealed class GatewayRequestHandler(
    GatePipeline pipeline,
    RequestSettlement settlement,
    IUpstreamForwarder forwarder,
    TimeProvider timeProvider)
{
    public async Task HandleAsync(HttpContext httpContext, string apiSlug)
    {
        httpContext.Request.Path.StartsWithSegments($"{GatewayRoutes.Prefix}/{apiSlug}", out var upstreamPath);
        var request = new RequestInfo(httpContext.Request.Method, upstreamPath.HasValue ? upstreamPath.Value! : "/");

        var context = new GateContext
        {
            ApiSlug = apiSlug,
            PresentedKey = httpContext.Request.Headers[GatewayHeaders.ApiKey].FirstOrDefault(),
            Now = timeProvider.GetUtcNow(),
        };

        var decision = await pipeline.RunAsync(context, httpContext.RequestAborted);
        WriteRateLimitHeaders(httpContext.Response, context.RateLimit);

        if (decision.Rejection is { } rejection)
        {
            settlement.SettleRejected(context, rejection, request);
            await WriteRejectionAsync(httpContext.Response, rejection);
            return;
        }

        var started = timeProvider.GetTimestamp();
        await forwarder.ForwardAsync(httpContext, context.Key!.TargetBaseUrl, upstreamPath);

        // the response is already out, so a client disconnect must not cancel this
        await settlement.SettleForwardedAsync(
            context,
            request,
            httpContext.Response.StatusCode,
            timeProvider.GetElapsedTime(started),
            CancellationToken.None);
    }

    private static void WriteRateLimitHeaders(HttpResponse response, RateLimitState? rateLimit)
    {
        if (rateLimit is null)
        {
            return;
        }

        response.Headers[GatewayHeaders.RateLimitLimit] = rateLimit.Limit.ToString(CultureInfo.InvariantCulture);
        response.Headers[GatewayHeaders.RateLimitRemaining] = rateLimit.Remaining.ToString(CultureInfo.InvariantCulture);
        response.Headers[GatewayHeaders.RateLimitReset] = rateLimit.ResetsAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
    }

    private static Task WriteRejectionAsync(HttpResponse response, GateRejection rejection)
    {
        if (rejection.RetryAfter is { } retryAfter)
        {
            response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        return ErrorResponses.WriteAsync(
            response,
            ErrorResponses.Problem(rejection.StatusCode, rejection.Code, rejection.Message));
    }
}
