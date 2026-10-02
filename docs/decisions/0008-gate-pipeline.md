# 0008. Admission checks as an ordered chain of gates

## Context

Before forwarding, the gateway authenticates the key and checks the rate limit, the credit balance
and the monthly quota. More checks are likely later (IP allow-lists, per-endpoint limits).

## Decision

Each check is a small class implementing `IRequestGate`:

```csharp
Task<GateResult> EvaluateAsync(GateContext context, CancellationToken cancellationToken);
```

`GatePipeline` runs the registered gates in order and stops at the first rejection. A rejection carries
the HTTP status, a stable `code`, a message, and (when the request is attributable) the outcome to
meter it under. Gates share a `GateContext`; the first gate puts the resolved key on it.

**The order is the DI registration order** in `Application/DependencyInjection.cs`:

1. `ApiKeyGate` - who is calling? Everything else needs the answer.
2. `RateLimitGate` - cheapest check (one Redis round trip) and the one that protects everything
   behind it from abuse.
3. `CreditGate` - reads the balance from MySQL.
4. `QuotaGate` - last, because it *consumes* quota by incrementing. Only a request that passed every
   other check, and will therefore be forwarded, should count against the month.

After the pipeline, `RequestSettlement` charges credits for successful responses and emits the usage
event. The HTTP layer (`GatewayRequestHandler`) knows nothing about limits; it only translates.

*Deviation from the plan:* the plan listed quota before credit. Implementing it showed that order
would burn monthly quota on requests then refused for lack of credits, so the two were swapped.

## Alternatives considered

- **ASP.NET Core middleware per check.** Works, but ties the rules to `HttpContext`, which makes them
  harder to unit test and impossible to reuse outside HTTP.
- **One method with four `if` blocks.** Shortest, and every new rule would mean editing it.
- **An explicit `Order` property on each gate.** Order would then be scattered over the gate classes;
  one registration block shows the whole pipeline at a glance.

## Consequences

- Adding a check: write a class, add one line. The pipeline, handler and existing gates are untouched.
- Each gate is unit tested in isolation with fakes.
- A request rejected by a later gate has still consumed a rate-limit slot. That is intended: rejected
  requests are still load.
