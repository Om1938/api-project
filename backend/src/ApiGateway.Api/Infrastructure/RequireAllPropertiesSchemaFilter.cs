using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiGateway.Api.Infrastructure;

internal sealed class RequireAllPropertiesSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concrete || concrete.Properties is null)
        {
            return;
        }

        concrete.Required ??= new HashSet<string>();
        foreach (var propertyName in concrete.Properties.Keys)
        {
            concrete.Required.Add(propertyName);
        }
    }
}
