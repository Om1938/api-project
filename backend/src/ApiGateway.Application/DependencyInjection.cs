using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Analytics;
using ApiGateway.Application.Apis;
using ApiGateway.Application.Auth;
using ApiGateway.Application.Billing;
using ApiGateway.Application.Common;
using ApiGateway.Application.Consumers;
using ApiGateway.Application.Gateway;
using ApiGateway.Application.Gateway.Gates;
using ApiGateway.Application.Keys;
using ApiGateway.Application.Tiers;
using ApiGateway.Application.Webhooks;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ApiGateway.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<OwnedApiGuard>();
        services.AddScoped<KeyContextRefresher>();
        services.AddScoped<UsageBreakdownBuilder>();
        services.AddScoped<UsageAggregator>();

        services.AddScoped<AuthService>();
        services.AddScoped<ApiService>();
        services.AddScoped<TierService>();
        services.AddScoped<ApiKeyService>();
        services.AddScoped<WebhookService>();
        services.AddScoped<ConsumerService>();
        services.AddScoped<ConsumerPortalService>();
        services.AddScoped<AnalyticsService>();
        services.AddScoped<CreditService>();

        services.AddGateway();

        return services;
    }

    private static void AddGateway(this IServiceCollection services)
    {
        // gates run in registration order
        services.AddScoped<IRequestGate, ApiKeyGate>();
        services.AddScoped<IRequestGate, RateLimitGate>();
        services.AddScoped<IRequestGate, CreditGate>();
        services.AddScoped<IRequestGate, QuotaGate>();

        services.AddScoped<GatePipeline>();
        services.AddScoped<RequestSettlement>();
        services.AddScoped<IQuotaUsageReader, QuotaUsageReader>();
    }
}
