using ApiGateway.Application.Keys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Api.Controllers;

[Route("api/apis/{apiId:guid}/keys")]
[Authorize(Roles = Roles.Owner)]
[Tags("Owner - API keys")]
public sealed class ApiKeysController(ApiKeyService keys) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApiKeyDto>>> List(Guid apiId, CancellationToken cancellationToken) =>
        Ok(await keys.ListAsync(apiId, cancellationToken));

    /// <summary>Issue a key to a consumer at a tier. The secret is returned only in this response.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<CreatedApiKeyDto>> Create(Guid apiId, CreateApiKeyRequest request, CancellationToken cancellationToken) =>
        Created(await keys.CreateAsync(apiId, request, cancellationToken));

    /// <summary>Permanently revoke a key. Takes effect on the next gateway request.</summary>
    [HttpPost("{keyId:guid}/revoke")]
    public async Task<ActionResult<ApiKeyDto>> Revoke(Guid apiId, Guid keyId, CancellationToken cancellationToken) =>
        Ok(await keys.RevokeAsync(apiId, keyId, cancellationToken));
}
