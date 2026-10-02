# 0005. Email/password with JWT for the dashboard

## Context

Owners and consumers need to sign in to the dashboard. OAuth/OIDC and complex RBAC are out of scope.

## Decision

- Accounts are **email + password**. Passwords are hashed with ASP.NET Core Identity's
  `PasswordHasher` (PBKDF2), used as a library behind the `IPasswordHasher` port; the Identity
  framework itself (stores, managers, cookies) is not used.
- Sign-in returns a **JWT** (HMAC-SHA256, 8 hours by default) carrying `sub`, `email` and `role`.
  The dashboard sends it as a bearer token.
- There are exactly **two roles**, `Owner` and `Consumer`, enforced with `[Authorize(Roles = ...)]`.
  A user has one role, chosen at registration.
- **Both roles self-register.** An owner issues a key to a consumer by choosing from the registered
  consumer accounts.
- Resource-level access ("is this *your* API?") is not a role question; it is enforced by
  `OwnedApiGuard` in the application layer, which reports someone else's API as *not found*.
- Login gives the same answer for an unknown email and a wrong password.

## Alternatives considered

- **ASP.NET Core Identity in full.** Brings lockout, confirmation, tokens and many tables that a demo
  does not need.
- **Cookie sessions.** Simpler for a browser-only app, but bearer tokens also make Swagger UI and the
  demo script trivial to use.
- **Owners creating consumer accounts.** Would require inviting and password-setting flows.

## Consequences

- No refresh tokens, password reset or email verification. A token is valid until it expires; there is
  no server-side revocation.
- The signing key comes from configuration (`Jwt:SigningKey`) and the app refuses to start if it is
  shorter than 32 characters. The compose file ships a demo key that must be replaced for real use.
- Any owner can list all consumer accounts (name and email). Acceptable for a single-tenant demo; a
  real product would need invitations or consumer-initiated subscriptions.
- The frontend keeps the token in `localStorage`, which is exposed to XSS; fine for a demo, a
  production app would prefer an HttpOnly cookie.
