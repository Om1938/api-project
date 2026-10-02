# 0003. MVC controllers documented with Swashbuckle

## Context

The PRD asks for Swagger API documentation. .NET 10 offers minimal APIs or controllers, and either
`Microsoft.AspNetCore.OpenApi` or Swashbuckle for the document.

## Decision

- **Controllers** for the management API. Attribute routing, `[Authorize(Roles = ...)]` per controller
  and action filters fit a CRUD-shaped API with shared conventions.
- **Swashbuckle** generates the OpenAPI document and serves Swagger UI at `/swagger`, with a JWT bearer
  scheme so "Authorize" works in the UI. XML doc comments on actions become operation descriptions.
- Cross-cutting behaviour is written once:
  - `ValidationFilter` runs the FluentValidation validator of any action argument.
  - `ApiControllerBase` maps `Result` to HTTP; `ErrorResponses` defines the RFC 7807 error body with a
    stable `code`.
  - `ProblemResponsesOperationFilter` documents the 400/401/403/404/409 responses from the shape of
    each operation, so actions carry no repeated `[ProducesResponseType]` for errors.
  - `RequireAllPropertiesSchemaFilter` marks DTO properties as required so generated TypeScript types
    are not all-optional ([0016](0016-frontend-stack.md)).
- Enums are serialised as strings.

**The gateway route is deliberately excluded from the Swagger document.** It is a catch-all proxy for
every HTTP method and arbitrary paths, which OpenAPI cannot describe meaningfully. It is documented in
[gateway-request-flow.md](../gateway-request-flow.md), and the Swagger description points there. The
plan originally listed an `X-API-Key` security scheme in Swagger; with the route excluded it would
have applied to nothing, so it was dropped.

## Alternatives considered

- **Minimal APIs.** Fewer files, but grouping conventions (filters, auth, response mapping) are less
  uniform than a base controller.
- **Built-in `Microsoft.AspNetCore.OpenApi` + a separate UI.** Works, but Swashbuckle gives document and
  UI in one package and is what "Swagger" means to most .NET developers.

## Consequences

- The error-response documentation is derived by rule, so it is slightly broader than reality (for
  example every write operation lists 409 even if it cannot conflict).
- Swagger UI is enabled in every environment, which is right for a demo and should be gated in production.
