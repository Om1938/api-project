using ApiGateway.Api.Gateway;
using ApiGateway.Api.Infrastructure;
using ApiGateway.Application;
using ApiGateway.Infrastructure;
using ApiGateway.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddWebApi(builder.Configuration);

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI(options => options.DocumentTitle = "API Gateway");
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGateway();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).ExcludeFromDescription();

app.Run();

public partial class Program; // for WebApplicationFactory
