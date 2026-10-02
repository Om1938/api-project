using ApiGateway.Application.Abstractions;
using ApiGateway.Infrastructure.Gateway;
using ApiGateway.Infrastructure.Persistence;
using ApiGateway.Infrastructure.Redis;
using ApiGateway.Infrastructure.Security;
using ApiGateway.Infrastructure.Usage;
using ApiGateway.Infrastructure.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ApiGateway.Infrastructure;

public static class DependencyInjection
{
    private static readonly MySqlServerVersion MySqlVersion = new(new Version(8, 4, 0));

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddPersistence(configuration);
        services.AddRedis(configuration);
        services.AddSecurity(configuration);
        services.AddUsageMetering();
        services.AddWebhooks(configuration);

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options => options.UseMySql(
            configuration.GetConnectionString("MySql"),
            MySqlVersion,
            mySql => mySql.EnableRetryOnFailure()));

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<ICreditLedger, EfCreditLedger>();
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.Section));
    }

    private static void AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(configuration.GetConnectionString("Redis")!);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<IFixedWindowCounter, RedisFixedWindowCounter>();

        services.AddSingleton<KeyContextCache>();
        services.AddSingleton<IKeyContextInvalidator>(provider => provider.GetRequiredService<KeyContextCache>());
        services.AddScoped<EfKeyContextResolver>();
        services.AddScoped<IKeyContextResolver, CachedKeyContextResolver>();
    }

    private static void AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Section))
            .Validate(jwt => jwt.SigningKey.Length >= 32, "Jwt:SigningKey must be at least 32 characters.")
            .ValidateOnStart();

        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
    }

    private static void AddUsageMetering(this IServiceCollection services)
    {
        services.AddSingleton<UsageChannel>();
        services.AddSingleton<IUsageSink>(provider => provider.GetRequiredService<UsageChannel>());
        services.AddHostedService<UsageWriterService>();
    }

    private static void AddWebhooks(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WebhookOptions>(configuration.GetSection(WebhookOptions.Section));

        services.AddSingleton<QuotaAlertChannel>();
        services.AddSingleton<IQuotaAlertSink>(provider => provider.GetRequiredService<QuotaAlertChannel>());
        services.AddHttpClient<IWebhookSender, WebhookSender>((provider, client) =>
            client.Timeout = provider.GetRequiredService<IOptions<WebhookOptions>>().Value.Timeout);
        services.AddHostedService<WebhookDispatcherService>();
    }
}
