using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiGateway.Api.Infrastructure;

internal sealed class ProblemResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var requiresAuth = metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any();
        var hasRouteParameters = context.ApiDescription.ParameterDescriptions.Any(p => p.Source.Id == "Path");
        var isWrite = !HttpMethods.IsGet(context.ApiDescription.HttpMethod ?? HttpMethods.Get);

        Add(isWrite, StatusCodes.Status400BadRequest, "The request is invalid.");
        Add(requiresAuth || isWrite, StatusCodes.Status401Unauthorized, "Missing, expired or invalid credentials.");
        Add(requiresAuth, StatusCodes.Status403Forbidden, "The signed-in user's role may not call this endpoint.");
        Add(hasRouteParameters, StatusCodes.Status404NotFound, "The resource does not exist or belongs to someone else.");
        Add(isWrite, StatusCodes.Status409Conflict, "The request conflicts with the current state.");

        void Add(bool applies, int status, string description)
        {
            if (!applies)
            {
                return;
            }

            operation.Responses ??= [];
            operation.Responses.TryAdd(status.ToString(), new OpenApiResponse
            {
                Description = description,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/problem+json"] = new()
                    {
                        Schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository),
                    },
                },
            });
        }
    }
}
