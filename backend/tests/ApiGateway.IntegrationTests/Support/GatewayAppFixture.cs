using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MySql;
using Testcontainers.Redis;

namespace ApiGateway.IntegrationTests.Support;

public sealed class GatewayAppFixture : IAsyncLifetime
{
    private readonly MySqlContainer _mySql = new MySqlBuilder("mysql:8.4").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();
    private WebApplicationFactory<Program> _factory = null!;

    public const int SeededHistoryDays = 14;

    public UpstreamStub Upstream { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_mySql.StartAsync(), _redis.StartAsync());
        Upstream = await UpstreamStub.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:MySql", _mySql.GetConnectionString());
            builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());
            builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-0123456789abcdef");
            builder.UseSetting("Webhooks:RetryBaseDelay", "00:00:00.050");
            builder.UseSetting("Seed:Enabled", "true");
            builder.UseSetting("Seed:SampleHistoryDays", SeededHistoryDays.ToString());
        });

        _factory.CreateClient().Dispose(); // boots the host, which runs migrations
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await Upstream.DisposeAsync();
        await Task.WhenAll(_mySql.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }

    public HttpClient CreateClient() => _factory.CreateClient();
}

[CollectionDefinition(Name)]
public sealed class GatewayAppCollection : ICollectionFixture<GatewayAppFixture>
{
    public const string Name = "gateway-app";
}
