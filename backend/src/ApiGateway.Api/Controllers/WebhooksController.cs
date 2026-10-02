using ApiGateway.Application.Webhooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Api.Controllers;

[Route("api/apis/{apiId:guid}/webhooks")]
[Authorize(Roles = Roles.Owner)]
[Tags("Owner - Webhooks")]
public sealed class WebhooksController(WebhookService webhooks) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WebhookDto>>> List(Guid apiId, CancellationToken cancellationToken) =>
        Ok(await webhooks.ListAsync(apiId, cancellationToken));

    /// <summary>Subscribe a URL to quota events (<c>quota.threshold_reached</c>, <c>quota.exceeded</c>) of this API.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<WebhookDto>> Create(Guid apiId, WebhookRequest request, CancellationToken cancellationToken) =>
        Created(await webhooks.CreateAsync(apiId, request, cancellationToken));

    [HttpPut("{webhookId:guid}")]
    public async Task<ActionResult<WebhookDto>> Update(Guid apiId, Guid webhookId, WebhookRequest request, CancellationToken cancellationToken) =>
        Ok(await webhooks.UpdateAsync(apiId, webhookId, request, cancellationToken));

    [HttpDelete("{webhookId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid apiId, Guid webhookId, CancellationToken cancellationToken) =>
        NoContent(await webhooks.DeleteAsync(apiId, webhookId, cancellationToken));

    /// <summary>The most recent delivery attempts for this API's webhooks, newest first.</summary>
    [HttpGet("deliveries")]
    public async Task<ActionResult<IReadOnlyList<WebhookDeliveryDto>>> Deliveries(Guid apiId, CancellationToken cancellationToken) =>
        Ok(await webhooks.ListDeliveriesAsync(apiId, cancellationToken));
}
