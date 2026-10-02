FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/ApiGateway.Domain/ApiGateway.Domain.csproj src/ApiGateway.Domain/
COPY src/ApiGateway.Application/ApiGateway.Application.csproj src/ApiGateway.Application/
COPY src/ApiGateway.Infrastructure/ApiGateway.Infrastructure.csproj src/ApiGateway.Infrastructure/
COPY src/ApiGateway.Api/ApiGateway.Api.csproj src/ApiGateway.Api/
RUN dotnet restore src/ApiGateway.Api/ApiGateway.Api.csproj

COPY src/ src/
RUN dotnet publish src/ApiGateway.Api/ApiGateway.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
LABEL org.opencontainers.image.source="https://github.com/Om1938/api-project"
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ApiGateway.Api.dll"]
