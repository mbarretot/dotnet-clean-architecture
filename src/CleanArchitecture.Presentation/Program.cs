using System.Reflection;
using CleanArchitecture.Application;
using CleanArchitecture.Infrastructure;
using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.Presentation.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

builder.Services.AddAuthenticationAndAuthorization();

builder.Services.AddApiDocumentation();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

var isRunningAsTheEntryPoint = Assembly.GetEntryAssembly() == typeof(Program).Assembly;

if (isRunningAsTheEntryPoint)
{
    await app.Services.ApplyPendingMigrationsAsync();
}

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapApi();

app.Run();

public partial class Program;
