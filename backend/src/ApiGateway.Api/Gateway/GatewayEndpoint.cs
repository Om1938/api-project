using Yarp.ReverseProxy.Forwarder;

namespace ApiGateway.Api.Gateway;

public static class GatewayRoutes
{
    public const string Prefix = "/gw";
    public const string Pattern = Prefix + "/{apiSlug}/{**path}";
}

public static class GatewayHeaders
{
    public const string ApiKey = "X-API-Key";
    public const string RateLimitLimit = "X-RateLimit-Limit";
    public const string RateLimitRemaining = "X-RateLimit-Remaining";
    public const string RateLimitReset = "X-RateLimit-Reset";
}

public static class GatewayEndpoint
{
    public static IServiceCollection AddGatewayEndpoint(this IServiceCollection services)
    {
        services.AddHttpForwarder();
        services.AddSingleton<IUpstreamForwarder, YarpUpstreamForwarder>();
        services.AddScoped<GatewayRequestHandler>();
        return services;
    }

    public static IEndpointConventionBuilder MapGateway(this IEndpointRouteBuilder endpoints) =>
        endpoints
            .Map(GatewayRoutes.Pattern, (HttpContext httpContext, string apiSlug, GatewayRequestHandler handler) =>
                handler.HandleAsync(httpContext, apiSlug))
            .AllowAnonymous()
            .ExcludeFromDescription();
}
