# 0006. API key format, hashing and show-once

## Context

API keys are bearer credentials for the gateway. They are checked on every proxied request, so the
check must be fast, and a database leak must not hand out working keys.

## Decision

- A key is `gw_` followed by 32 random bytes (256 bits) in URL-safe base64, for example
  `gw_SKIRdCswhvTyC_byMXOEJZ3PasmPKgDWUo7UQbfhpWs`. The prefix makes keys recognisable in logs and
  secret scanners.
- Only two things are stored: the **SHA-256 hash** (unique index, used for lookup) and the **first 11
  characters** (`KeyPrefix`, for display).
- The **plaintext is returned exactly once**, in the response to the create call. The dashboard shows
  it in a dialog with a copy button and says it cannot be shown again.
- Consumers see their keys **masked** (prefix, API, tier, status, quota), not the secret; the owner
  hands the secret over out of band.
- A key binds one consumer to one API at one tier. **Revocation is permanent**; to change tier, issue
  a new key.
- Generation and hashing live in one domain type, `ApiKeySecret`.

## Alternatives considered

- **A slow password hash (bcrypt/PBKDF2).** Those exist to slow down guessing of low-entropy secrets.
  A 256-bit random key cannot be guessed, and a slow hash would add tens of milliseconds to every
  gateway request.
- **Storing the key encrypted so consumers can reveal it later.** More convenient, but then the server
  holds recoverable credentials and a key-management problem.
- **Letting consumers rotate their own key.** A good next feature; not required by the PRD.

## Consequences

- A lost key cannot be recovered; the owner revokes it and issues a new one.
- Lookup by hash is a single indexed equality match, and is cached ([0015](0015-key-context-caching.md)).
