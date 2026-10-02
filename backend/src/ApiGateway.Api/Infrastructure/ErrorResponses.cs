using ApiGateway.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Api.Infrastructure;

public static class ErrorResponses
{
    public const string CodeExtension = "code";

    public static ProblemDetails Problem(int status, string code, string detail) => new()
    {
        Status = status,
        Title = Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(status),
        Detail = detail,
        Extensions = { [CodeExtension] = code },
    };

    public static ProblemDetails Problem(Error error) => Problem(StatusOf(error.Type), error.Code, error.Message);

    public static int StatusOf(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError,
    };

    public static Task WriteAsync(HttpResponse response, ProblemDetails problem)
    {
        response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }
}

internal sealed class UnhandledExceptionHandler(ILogger<UnhandledExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        await ErrorResponses.WriteAsync(
            httpContext.Response,
            ErrorResponses.Problem(StatusCodes.Status500InternalServerError, "internal_error", "An unexpected error occurred."));
        return true;
    }
}
