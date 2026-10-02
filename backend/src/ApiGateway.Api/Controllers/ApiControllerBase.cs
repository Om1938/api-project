using ApiGateway.Api.Infrastructure;
using ApiGateway.Application.Common;
using ApiGateway.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<T> Ok<T>(Result<T> result) =>
        result.IsSuccess ? base.Ok(result.Value) : Failure(result.Error!);

    protected ActionResult<T> Created<T>(Result<T> result) =>
        result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : Failure(result.Error!);

    protected IActionResult NoContent(Result result) =>
        result.IsSuccess ? NoContent() : Failure(result.Error!);

    private ObjectResult Failure(Error error)
    {
        var problem = ErrorResponses.Problem(error);
        return new ObjectResult(problem) { StatusCode = problem.Status, ContentTypes = { "application/problem+json" } };
    }
}

public static class Roles
{
    public const string Owner = nameof(UserRole.Owner);
    public const string Consumer = nameof(UserRole.Consumer);
}
