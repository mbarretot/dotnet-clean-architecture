# API and authentication guide

[Overview](../README.md) · [Development and delivery](development-guide.md) · [Decisions](adr/README.md)

## Local addresses

Aspire: `http://localhost:5236/scalar/v1`. Compose: `http://localhost:8080/scalar/v1`; OpenAPI: `http://localhost:8080/openapi/v1.json`. Keycloak: `http://localhost:8180` (admin/admin, development only). Startup applies migrations.

## Separate PostgreSQL instance

```bash
dotnet run --project src/CleanArchitecture.Presentation
```

Configure `ConnectionStrings:Database` and [local tokens](#local-api-alone-with-dotnet-user-jwts) before running. Startup applies pending migrations.

For an explicit migration step:

```bash
dotnet tool restore
dotnet ef database update \
  --project src/CleanArchitecture.Infrastructure \
  --startup-project src/CleanArchitecture.Infrastructure \
  --connection "<your PostgreSQL connection string>"
```

The design-time factory otherwise targets `localhost:5432` with development credentials; Compose does not expose its database port to the host by default.



### Try one complete business flow

1. Open Scalar and sign in as `alice` / `alice` with the write and fulfillment scopes ([local authentication](#local-keycloak-docker-compose-and-aspire)).
2. Create a product using `POST /api/products`; include `stockQuantity: 10` and copy the returned UUID.
3. Place an order with that UUID and quantity `2`. Stock becomes `8`; the order keeps the name and price at purchase time.
4. Use the returned order UUID to pay → ship → complete; or cancel **before shipment** to restore stock.

<details>
<summary><strong>Example request bodies</strong></summary>

Product `POST /api/products`:

```json
{
  "name": "Mechanical Keyboard",
  "description": "Hot-swappable keyboard",
  "price": 89.99,
  "currency": "USD",
  "sku": "KB-100",
  "stockQuantity": 10
}
```

Order `POST /api/orders` (replace the placeholder with the created UUID):

```json
{
  "lines": [{ "productId": "<created-product-id>", "quantity": 2 }]
}
```

- Create responses contain a UUID, not an `{ "id": ... }` object.
- Lifecycle routes: `POST /api/orders/{id}/pay`, `/ship`, `/complete`, `/cancel`.
- The [HTTP collection](../src/CleanArchitecture.Presentation/CleanArchitecture.Presentation.http) also works; create/set nonzero stock before ordering.

</details>

## API surface

| Method | Route | Purpose | Requires |
|---|---|---|---|
| `GET` | `/api/products` | List products: `search`, `minPrice`, `maxPrice`, `sort` (`name`, `-name`, `price`, `-price`), pagination | Authenticated user |
| `GET` | `/api/products/{id}` | Get one product (cached) | Authenticated user |
| `POST` | `/api/products` | Create a product, with optional initial `stockQuantity` | `products:write` scope |
| `PUT` | `/api/products/{id}` | Update a product | `products:write` scope |
| `PUT` | `/api/products/{id}/stock` | Set the units on hand | `products:write` scope |
| `DELETE` | `/api/products/{id}` | Soft-delete a product (then reads as `404`) | `products:write` scope |
| `GET` | `/api/orders` | List my orders with pagination, newest first | Authenticated user |
| `GET` | `/api/orders/{id}` | Get one of my orders (someone else's reads as `404`) | Authenticated user |
| `POST` | `/api/orders` | Place an order, snapshotting names and prices and reserving stock (short: `409`) | `orders:write` scope |
| `POST` | `/api/orders/{id}/pay` | Pay one of my placed orders | `orders:write` scope |
| `POST` | `/api/orders/{id}/cancel` | Cancel one of my placed or paid orders and release its stock | `orders:write` scope |
| `POST` | `/api/orders/{id}/ship` | Ship a paid order | `orders:fulfill` scope |
| `POST` | `/api/orders/{id}/complete` | Complete a shipped order | `orders:fulfill` scope |

- Lifecycle violations return `409`; cancellation is allowed only while placed/paid.
- API version defaults to `1.0`. Select it with `api-version` (query) or `X-Api-Version` (header); unsupported versions return `400`.

## Authentication

Configure provider-agnostic JWT validation through `Authentication:Schemes:Bearer`. Without an issuer, startup succeeds but protected requests return `401`.

- **Default:** authentication required unless `AllowAnonymous()` is explicit.
- **Public:** `/health`, `/alive`, OpenAPI, and Development-only Scalar; a convention test pins this list.
- **Claims:** `sub` identifies/audits the caller; `scope` accepts space-delimited or repeated claims.
- **Errors:** RFC 9457 `application/problem+json` for `401`/`403`; challenges retain `WWW-Authenticate`. OpenAPI documents these responses.
- **Permissions:** scopes are listed [above](#api-surface); handlers enforce order ownership, except cross-customer fulfillment.

### Local: Keycloak (Docker Compose and Aspire)

Both setups import the [development realm](../deploy/keycloak/clean-architecture-realm.json) on port `8180`. Tokens carry audience `clean-architecture-api` and granted scopes.

| Resource | Compose | Aspire |
|---|---|---|
| Issuer | `http://localhost:8180/realms/clean-architecture` | Same |
| Scalar | `http://localhost:8080/scalar/v1` | `http://localhost:5236/scalar/v1` |

Sign in from Scalar’s **OAuth2** scheme as `alice` / `alice`. Select the product-write, order-write and fulfillment scopes for the walkthrough.

> [!WARNING]
> All committed credentials are **development-only**. Never reuse them in production; the two orchestrators also share port 8180 and cannot run simultaneously.

<details>
<summary><strong>Machine clients, token command and issuer configuration</strong></summary>

| Client / secret | Granted scopes | Behavior |
|---|---|---|
| `clean-architecture-service` / `dev-only-service-secret` | `products:write`, `orders:write`, `orders:fulfill` | Read/write/fulfill |
| `clean-architecture-reader` / `dev-only-reader-secret` | None | Reads succeed; writes return `403` |
| Public `scalar` client | Write/fulfill scopes optional | Authorization Code + PKCE |

The following command needs `curl` and `jq` and targets Compose:

```bash
TOKEN=$(curl -s http://localhost:8180/realms/clean-architecture/protocol/openid-connect/token \
  -d grant_type=client_credentials \
  -d client_id=clean-architecture-service -d client_secret=dev-only-service-secret | jq -r .access_token)

curl -i http://localhost:8080/api/products -H "Authorization: Bearer $TOKEN"
```

- Scalar uses optional `OpenApi:OAuth2` settings: `AuthorizationUrl`, `TokenUrl`, `ClientId`. Without them, use its Bearer field.
- The [HTTP collection](../src/CleanArchitecture.Presentation/CleanArchitecture.Presentation.http) fetches/reuses a Keycloak token.
- Compose retrieves keys via `http://keycloak:8080`, but validates the host-facing issuer pinned by `KC_HOSTNAME`; `KC_HOSTNAME_BACKCHANNEL_DYNAMIC` keeps internal discovery working.
- Only local orchestrators disable `RequireHttpsMetadata`.

</details>

### Local: API alone with `dotnet user-jwts`

No identity-provider container is needed. This tool stores signing keys in user secrets and issuer/audience settings in `appsettings.Development.json`:

```bash
dotnet user-jwts create --project src/CleanArchitecture.Presentation --scope products:write --scope orders:write --scope orders:fulfill
```

Omit `--scope` for read-only access; a configured PostgreSQL database is still required.

### Production

Set the identity provider through environment variables (`__` separates configuration levels):

| Key | Purpose |
|---|---|
| `Authentication__Schemes__Bearer__Authority` | OIDC authority for signing-key discovery |
| `Authentication__Schemes__Bearer__ValidAudiences__0` | Accepted audience; add `__1`, … for more |
| `Authentication__Schemes__Bearer__ValidIssuer` | Optional override when token `iss` differs from authority |
