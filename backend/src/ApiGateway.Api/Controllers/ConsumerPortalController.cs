using ApiGateway.Application.Analytics;
using ApiGateway.Application.Billing;
using ApiGateway.Application.Consumers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Api.Controllers;

[Route("api/me")]
[Authorize(Roles = Roles.Consumer)]
[Tags("Consumer")]
public sealed class ConsumerPortalController(ConsumerPortalService portal, CreditService credits) : ApiControllerBase
{
    /// <summary>Keys assigned to the signed-in consumer, with limits and this month's quota consumption.</summary>
    [HttpGet("keys")]
    public async Task<ActionResult<IReadOnlyList<MyKeyDto>>> Keys(CancellationToken cancellationToken) =>
        Ok(await portal.ListKeysAsync(cancellationToken));

    /// <summary>The consumer's own usage: totals by outcome and a time series.</summary>
    /// <param name="from">Start of the range (ISO 8601). Defaults to 24 hours before <paramref name="to"/>.</param>
    /// <param name="to">End of the range (ISO 8601). Defaults to now.</param>
    [HttpGet("usage")]
    public async Task<ActionResult<UsageReportDto>> Usage(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken) =>
        Ok(await portal.GetUsageAsync(from, to, cancellationToken));

    /// <summary>Credit balance, credits spent this month and recent top-ups.</summary>
    [HttpGet("credits")]
    public async Task<ActionResult<CreditAccountDto>> Credits(CancellationToken cancellationToken) =>
        Ok(await credits.GetAccountAsync(cancellationToken));

    /// <summary>Add credits. This is a mock purchase: no payment is taken.</summary>
    [HttpPost("credits/top-up")]
    public async Task<ActionResult<CreditAccountDto>> TopUp(TopUpRequest request, CancellationToken cancellationToken) =>
        Ok(await credits.TopUpAsync(request, cancellationToken));
}
