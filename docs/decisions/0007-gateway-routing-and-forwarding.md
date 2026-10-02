# 0007. Routing by slug and forwarding with YARP

## Context

The gateway must map an incoming request to a registered API and proxy it: any method, any path,
streaming bodies, correct header handling, sensible behaviour when the upstream fails.

## Decision

- **Route:** `ANY /gw/{apiSlug}/{**path}`. Each API has a unique, immutable, URL-safe **slug**
  (given by the owner or derived from the name). Everything after the slug, plus the query string, is
  appended to the API's target base URL.
- **Slug format:** lowercase letters and digits separated by single hyphens, at most 64 characters
  (`Slugs`). A name with no letters or digits cannot produce one and is rejected.
- **Credential:** the `X-API-Key` request header.
- YARP appends the request path to the destination, so the handler strips the `/gw/{slug}` prefix
  from the path before forwarding.
- **Forwarding:** YARP's `IHttpForwarder` (direct forwarding), wrapped behind `IUpstreamForwarder`.
  A small transformer removes `X-API-Key` and lets `Host` follow the destination.
- The upstream `HttpClient` does not follow redirects, use cookies, use a system proxy or decompress,
  so responses pass through unmodified. Connect timeout 10 s, activity timeout 30 s.
- The key must belong to the API named in the URL; a valid key for another API gets `403`.

## Alternatives considered

- **Hand-written proxying with `HttpClient`.** Looks easy and is full of traps: hop-by-hop headers,
  streaming without buffering, request aborts, trailers, upgrade requests. YARP already solves these.
- **YARP's full reverse-proxy pipeline (routes/clusters).** Built for configuration-driven routing; here
  the destination comes from the database per request, which is exactly what direct forwarding is for.
- **Identifying the API by key alone (`/gw/{**path}`).** Shorter URLs, but the URL would no longer say
  which API is called, and a mistyped key would hit a different API.
- **Subdomain per API.** Nicer URLs, needs wildcard DNS; impractical for a local demo.

## Consequences

- Upstream connection failures become `502`, timeouts `504`; both are metered as failed requests.
- The slug cannot be changed after creation, because consumers have it in their URLs.
- Target URLs are supplied by owners; see [0019](0019-outbound-url-validation.md).
