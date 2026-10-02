using System.Text.Json.Serialization;
using ApiGateway.Api.Gateway;
using ApiGateway.Application.Abstractions;
using ApiGateway.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace ApiGateway.Api.Infrastructure;

public static class WebApiServices
{
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddWebApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        services
            .AddControllers(options => options.Filters.Add<ValidationFilter>())
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddProblemDetails();
        services.AddExceptionHandler<UnhandledExceptionHandler>();

        services.AddJwtAuthentication();
        services.AddCorsPolicy(configuration);
        services.AddSwagger();
        services.AddGatewayEndpoint();

        return services;
    }

    private static void AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = jwt.CreateSecurityKey(),
                    ValidateIssuerSigningKey = true,
                    NameClaimType = JwtClaims.UserId,
                    RoleClaimType = JwtClaims.Role,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorization();
    }

    private static void AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddDefaultPolicy(policy =>
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));
    }

    private static void AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "API Gateway - Management API",
                Version = "v1",
                Description =
                    "Dashboard endpoints for API owners and consumers (JWT bearer). "
                    + $"Proxied traffic goes through `{GatewayRoutes.Pattern}` with an `{GatewayHeaders.ApiKey}` header "
                    + "and is documented in docs/gateway-request-flow.md.",
            });

            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste the accessToken returned by /api/auth/login.",
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerScheme, document)] = [],
            });

            options.SchemaFilter<RequireAllPropertiesSchemaFilter>();
            options.OperationFilter<ProblemResponsesOperationFilter>();
            options.SupportNonNullableReferenceTypes();

            var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(WebApiServices).Assembly.GetName().Name}.xml");
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });
    }
}
