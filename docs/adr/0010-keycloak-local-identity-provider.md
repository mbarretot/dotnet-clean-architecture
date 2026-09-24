# 10. Keycloak as the local identity provider

- **Status:** Accepted
- **Date:** 2026-09-24

## Context

The API only validates tokens ([ADR-0009](0009-jwt-bearer-scopes-secure-by-default.md)); something must issue them
locally so developers can exercise real OAuth flows (client credentials, Authorization Code + PKCE from Scalar)
under both Docker Compose and Aspire, with no cloud tenant and no manual setup.

## Decision

Run Keycloak in both local orchestrators and import one realm defined as code.

- [`deploy/keycloak/clean-architecture-realm.json`](../../deploy/keycloak/clean-architecture-realm.json) defines the
  `clean-architecture` realm: an audience mapper that sets `aud = clean-architecture-api`, the `products:write`
  and `orders:write` client scopes, confidential clients `clean-architecture-service` (write) and
  `clean-architecture-reader` (read only), the public `scalar` client, and the test user `alice`. Every secret in it is a committed dev-only value.
- [`docker-compose.yml`](../../docker-compose.yml) runs `keycloak:26.6` with `start-dev --import-realm` on host port
  8180. `KC_HOSTNAME=http://localhost:8180` pins the token issuer, and `KC_HOSTNAME_BACKCHANNEL_DYNAMIC=true` lets
  the API container fetch signing keys from `http://keycloak:8080` (its `Authority`) while validating
  `ValidIssuers` against the host-facing URL.
- [`AppHost.cs`](../../src/CleanArchitecture.AppHost/AppHost.cs) adds Keycloak with `WithRealmImport` on the same
  fixed port. The API runs on the host there, so one URL serves as both authority and issuer.
- Both set `RequireHttpsMetadata=false` and the Scalar OAuth2 settings (`OpenApi:OAuth2:*`); nothing in the API's
  own `appsettings.json` refers to Keycloak.

## Consequences

**Positive**

- `docker compose up` or `aspire run` gives working tokens for writer, reader, and interactive user scenarios.
- The realm is versioned and reviewed like code; the setup is reproducible.
- Keycloak stays a local detail: production points `Authentication:Schemes:Bearer` at any issuer.

**Negative**

- An extra, fairly heavy container (slower first start, more memory).
- The issuer/backchannel split in Compose is subtle; changing ports or hostnames breaks token validation.
- The fixed port 8180 means Compose and Aspire cannot run at the same time.
- The Aspire Keycloak integration (`Aspire.Hosting.Keycloak`) is a preview package
  ([`Directory.Packages.props`](../../Directory.Packages.props)).
- Committed credentials are safe only while they stay dev-only.

## Alternatives considered

- **`dotnet user-jwts` only.** No container, but no real OAuth flows; still supported for running the API alone.
- **A cloud tenant (Entra ID, Auth0).** Production-like, but requires accounts and secrets for every contributor.
- **A lighter IdP or mock OIDC server.** Smaller footprint; less representative of a real authorization server.
