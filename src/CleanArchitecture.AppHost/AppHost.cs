var builder = DistributedApplication.CreateBuilder(args);

// Name MUST stay "Database": Aspire injects it as ConnectionStrings__Database, which Infrastructure reads by that key.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("Database");

var api = builder.AddProject<Projects.CleanArchitecture_Presentation>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
