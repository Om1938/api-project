# 0019. Validation of owner-supplied URLs (SSRF stance)

## Context

Two features make the backend send HTTP requests to URLs that users type in: an API's **target base
URL** (the gateway forwards to it) and a **webhook URL** (the dispatcher POSTs to it). That is a
server-side request forgery (SSRF) surface: an owner could point either at an internal address and
use the gateway to reach services it should not.

## Decision

- Both URLs must be **absolute `http` or `https` URLs** of at most 2048 characters. Anything else is
  rejected with a validation error. The rule is defined once (`ValidationRules.HttpUrl`).
- **Private, loopback and link-local destinations are allowed.** The demo depends on it: the bundled
  upstream is `http://upstream:8080` on the Docker network, and in development `http://localhost:8081`.
- The gateway does not follow redirects, so a public URL cannot bounce a request to an internal one.

This is a conscious demo-scope choice, not an oversight.

## Alternatives considered

- **Blocking private address ranges.** The right default for a multi-tenant deployment, done by
  resolving the host and rejecting RFC 1918, loopback, link-local and metadata addresses at connect
  time (not just at validation time, to resist DNS rebinding). It would break the local demo unless
  made configurable.
- **An allow-list of upstream hosts.** Safest, least flexible.

## Consequences

- Anyone who can register as an owner can make the backend issue requests to hosts reachable from it,
  and read the responses through the gateway. **Do not expose this build to untrusted users.**
- Hardening path, in order: a configuration switch that blocks non-public destinations, enforced in
  the `SocketsHttpHandler.ConnectCallback` of both outbound clients; then restricting who may register
  as an owner.
