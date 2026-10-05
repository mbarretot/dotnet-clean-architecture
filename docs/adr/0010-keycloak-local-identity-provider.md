# 10. Keycloak as the local identity provider

- **Status:** Accepted
- **Date:** 2026-09-24

## Context

Local development needs real client-credentials and interactive PKCE flows without a cloud tenant. The API validates tokens; a separate provider must issue them.

## Decision

Run Keycloak under Aspire and Compose with one imported realm-as-code. Keep provider URLs and Scalar OAuth settings in orchestrator configuration rather than API appsettings.

## Outcome

The realm supplies the API audience, scopes, writer/reader clients, public Scalar client and Alice test user. Compose pins the host-facing issuer while using its internal authority/backchannel for key discovery.

## Consequences

### Benefits

- One-command setups support machine and interactive authentication.
- Realm changes are versioned; production may use another issuer.

### Trade-offs

- The extra container increases startup/resource costs; fixed port 8180 prevents simultaneous Aspire/Compose use.
- Hostname/backchannel differences are easy to misconfigure; Aspire uses a preview Keycloak integration.
- Committed credentials and disabled HTTPS metadata checks are development-only.

## Alternatives

- **user-jwts only:** supported for API-only runs, but cannot exercise OAuth flows.
- **Cloud tenant:** realistic, but requires contributor accounts/secrets.
- **Mock/lighter issuer:** smaller, but less representative.

## References

- [Realm](../../deploy/keycloak/clean-architecture-realm.json)
- [Compose identity configuration](../../docker-compose.yml)
- [Aspire identity configuration](../../src/CleanArchitecture.AppHost/AppHost.cs)
- [Integration package pin](../../Directory.Packages.props)
- [ADR-0009](0009-jwt-bearer-scopes-secure-by-default.md)
