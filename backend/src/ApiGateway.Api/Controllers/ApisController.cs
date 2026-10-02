using ApiGateway.Application.Apis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Api.Controllers;

[Route("api/apis")]
[Authorize(Roles = Roles.Owner)]
[Tags("Owner - APIs")]
public sealed class ApisController(ApiService apis) : ApiControllerBase
{
    /// <summary>APIs registered by the signed-in owner.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApiDto>>> List(CancellationToken cancellationToken) =>
        Ok(await apis.ListAsync(cancellationToken));

    [HttpGet("{apiId:guid}")]
    public async Task<ActionResult<ApiDto>> Get(Guid apiId, CancellationToken cancellationToken) =>
        Ok(await apis.GetAsync(apiId, cancellationToken));

    /// <summary>Register an upstream API. It becomes reachable at <c>/gw/{slug}/...</c>.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiDto>> Create(CreateApiRequest request, CancellationToken cancellationToken) =>
        Created(await apis.CreateAsync(request, cancellationToken));

    [HttpPut("{apiId:guid}")]
    public async Task<ActionResult<ApiDto>> Update(Guid apiId, UpdateApiRequest request, CancellationToken cancellationToken) =>
        Ok(await apis.UpdateAsync(apiId, request, cancellationToken));

    /// <summary>Delete an API together with its tiers, keys and webhooks. Usage history is kept.</summary>
    [HttpDelete("{apiId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid apiId, CancellationToken cancellationToken) =>
        NoContent(await apis.DeleteAsync(apiId, cancellationToken));
}
