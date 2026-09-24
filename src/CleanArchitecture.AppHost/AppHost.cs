var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

// Must stay "Database": Infrastructure reads ConnectionStrings:Database.
var database = postgres.AddDatabase("Database");

var keycloak = builder.AddKeycloak("keycloak", port: 8180)
    .WithRealmImport("../../deploy/keycloak");

var realm = ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/clean-architecture");

var api = builder.AddProject<Projects.CleanArchitecture_Presentation>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithEnvironment("Authentication__Schemes__Bearer__Authority", realm)
    .WithEnvironment("Authentication__Schemes__Bearer__ValidIssuers__0", realm)
    .WithEnvironment("Authentication__Schemes__Bearer__ValidAudiences__0", "clean-architecture-api")
    .WithEnvironment("Authentication__Schemes__Bearer__RequireHttpsMetadata", "false")
    .WithEnvironment("OpenApi__OAuth2__AuthorizationUrl", ReferenceExpression.Create($"{realm}/protocol/openid-connect/auth"))
    .WithEnvironment("OpenApi__OAuth2__TokenUrl", ReferenceExpression.Create($"{realm}/protocol/openid-connect/token"))
    .WithEnvironment("OpenApi__OAuth2__ClientId", "scalar")
    .WithHttpHealthCheck("/health");

builder.Build().Run();
