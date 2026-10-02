using ApiGateway.Application.Analytics;
using ApiGateway.Application.Consumers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Api.Controllers;

[Route("api")]
[Authorize(Roles = Roles.Owner)]
[Tags("Owner - Analytics")]
public sealed class OwnerInsightsController(AnalyticsService analytics, ConsumerService consumers) : ApiControllerBase
{
    /// <summary>Usage across the owner's APIs: totals by outcome, a time series and a per-consumer breakdown.</summary>
    /// <param name="apiId">Narrow the report to one API.</param>
    /// <param name="from">Start of the range (ISO 8601). Defaults to 24 hours before <paramref name="to"/>.</param>
    /// <param name="to">End of the range (ISO 8601). Defaults to now.</param>
    [HttpGet("analytics")]
    public async Task<ActionResult<OwnerAnalyticsDto>> Analytics(
        [FromQuery] Guid? apiId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken) =>
        Ok(await analytics.GetAsync(apiId, from, to, cancellationToken));

    /// <summary>Consumer accounts that can be given a key, with how many active keys each holds on the owner's APIs.</summary>
    [HttpGet("consumers")]
    public async Task<ActionResult<IReadOnlyList<ConsumerDto>>> Consumers(CancellationToken cancellationToken) =>
        Ok(await consumers.ListAsync(cancellationToken));
}
