var builder = DistributedApplication.CreateBuilder(args);

// Name MUST stay "Database": Aspire injects it as ConnectionStrings__Database, which Infrastructure reads by that key.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("Database");

// Local identity provider with the same realm Docker Compose imports. Fixed host port 8180 so token issuers and the
// Scalar client's redirect URIs match both setups (don't run Compose and Aspire at the same time).
var keycloak = builder.AddKeycloak("keycloak", port: 8180)
    .WithRealmImport("../../deploy/keycloak");

// The API runs on the host, so it reaches Keycloak through the same URL the browser and curl use: that URL is both
// the metadata authority and the token issuer. Aspire serves it over HTTPS with the ASP.NET Core dev certificate
// (https://localhost:8180) when one is trusted.
var realm = ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/clean-architecture");

var api = builder.AddProject<Projects.CleanArchitecture_Presentation>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithEnvironment("Authentication__Schemes__Bearer__Authority", realm)
    .WithEnvironment("Authentication__Schemes__Bearer__ValidIssuers__0", realm)
    .WithEnvironment("Authentication__Schemes__Bearer__ValidAudiences__0", "clean-architecture-api")
    // Local container only: tolerates the plain-HTTP fallback when no dev certificate is available.
    .WithEnvironment("Authentication__Schemes__Bearer__RequireHttpsMetadata", "false")
    .WithEnvironment("OpenApi__OAuth2__AuthorizationUrl", ReferenceExpression.Create($"{realm}/protocol/openid-connect/auth"))
    .WithEnvironment("OpenApi__OAuth2__TokenUrl", ReferenceExpression.Create($"{realm}/protocol/openid-connect/token"))
    .WithEnvironment("OpenApi__OAuth2__ClientId", "scalar")
    .WithHttpHealthCheck("/health");

builder.Build().Run();
