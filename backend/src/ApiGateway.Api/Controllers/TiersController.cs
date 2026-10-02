using ApiGateway.Application.Tiers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Api.Controllers;

[Route("api/apis/{apiId:guid}/tiers")]
[Authorize(Roles = Roles.Owner)]
[Tags("Owner - Tiers")]
public sealed class TiersController(TierService tiers) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TierDto>>> List(Guid apiId, CancellationToken cancellationToken) =>
        Ok(await tiers.ListAsync(apiId, cancellationToken));

    /// <summary>Define an access tier: requests per minute, monthly quota and credit cost per request.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<TierDto>> Create(Guid apiId, TierRequest request, CancellationToken cancellationToken) =>
        Created(await tiers.CreateAsync(apiId, request, cancellationToken));

    /// <summary>Change a tier. New limits apply to its keys from the next request.</summary>
    [HttpPut("{tierId:guid}")]
    public async Task<ActionResult<TierDto>> Update(Guid apiId, Guid tierId, TierRequest request, CancellationToken cancellationToken) =>
        Ok(await tiers.UpdateAsync(apiId, tierId, request, cancellationToken));

    /// <summary>Delete a tier. Refused while keys are still assigned to it.</summary>
    [HttpDelete("{tierId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid apiId, Guid tierId, CancellationToken cancellationToken) =>
        NoContent(await tiers.DeleteAsync(apiId, tierId, cancellationToken));
}
