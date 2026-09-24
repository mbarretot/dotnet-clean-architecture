using System.Reflection;
using CleanArchitecture.Application;
using CleanArchitecture.Infrastructure;
using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.Presentation.Middleware;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

var isRunningAsTheEntryPoint = Assembly.GetEntryAssembly() == typeof(Program).Assembly;

if (isRunningAsTheEntryPoint)
{
    await app.Services.ApplyPendingMigrationsAsync();
}

app.UseExceptionHandler();

app.MapDefaultEndpoints();

app.MapEndpoints();

app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}

app.Run();

/// <summary>Exposes the implicit Program class to <c>WebApplicationFactory&lt;Program&gt;</c> in tests.</summary>
public partial class Program;
