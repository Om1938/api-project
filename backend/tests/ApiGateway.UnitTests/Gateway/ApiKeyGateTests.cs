using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Gateway;
using ApiGateway.Application.Gateway.Gates;
using ApiGateway.Domain.Security;
using NSubstitute;

namespace ApiGateway.UnitTests.Gateway;

public class ApiKeyGateTests
{
    private readonly IKeyContextResolver _resolver = Substitute.For<IKeyContextResolver>();

    private Task<GateResult> EvaluateAsync(GateContext context) =>
        new ApiKeyGate(_resolver).EvaluateAsync(context, CancellationToken.None);

    private void ResolverReturns(KeyContext? key) =>
        _resolver.ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(key);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Missing_key_is_rejected_with_401_without_a_lookup(string? presentedKey)
    {
        var result = await EvaluateAsync(GateTestData.Context(presentedKey: presentedKey));

        Assert.Equal(401, result.Rejection!.StatusCode);
        Assert.Equal("missing_api_key", result.Rejection.Code);
        await _resolver.DidNotReceiveWithAnyArgs().ResolveAsync(default!, default);
    }

    [Fact]
    public async Task Unknown_key_is_rejected_with_401()
    {
        ResolverReturns(null);

        var result = await EvaluateAsync(GateTestData.Context());

        Assert.Equal(401, result.Rejection!.StatusCode);
        Assert.Equal("invalid_api_key", result.Rejection.Code);
    }

    [Fact]
    public async Task Revoked_key_is_rejected_with_401()
    {
        ResolverReturns(GateTestData.Key(keyIsActive: false));

        var result = await EvaluateAsync(GateTestData.Context());

        Assert.Equal("revoked_api_key", result.Rejection!.Code);
    }

    [Fact]
    public async Task Key_for_another_api_is_rejected_with_403()
    {
        ResolverReturns(GateTestData.Key(apiSlug: "weather"));

        var result = await EvaluateAsync(GateTestData.Context(apiSlug: "payments"));

        Assert.Equal(403, result.Rejection!.StatusCode);
        Assert.Equal("key_not_valid_for_api", result.Rejection.Code);
    }

    [Fact]
    public async Task Disabled_api_is_rejected_with_503()
    {
        ResolverReturns(GateTestData.Key(apiIsActive: false));

        var result = await EvaluateAsync(GateTestData.Context());

        Assert.Equal(503, result.Rejection!.StatusCode);
    }

    [Fact]
    public async Task Valid_key_is_allowed_looked_up_by_hash_and_put_on_the_context()
    {
        var key = GateTestData.Key();
        ResolverReturns(key);
        var context = GateTestData.Context(presentedKey: "gw_secret");

        var result = await EvaluateAsync(context);

        Assert.True(result.IsAllowed);
        Assert.Same(key, context.Key);
        await _resolver.Received(1).ResolveAsync(ApiKeySecret.ComputeHash("gw_secret"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Authentication_rejections_are_not_metered()
    {
        ResolverReturns(null);

        var result = await EvaluateAsync(GateTestData.Context());

        Assert.Null(result.Rejection!.Outcome);
    }
}
