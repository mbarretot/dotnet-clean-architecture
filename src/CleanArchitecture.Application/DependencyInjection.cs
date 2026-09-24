using System.Reflection;
using CleanArchitecture.Application.Behaviors;
using CleanArchitecture.SharedKernel.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var applicationAssembly = Assembly.GetExecutingAssembly();

        services.AddMediator(applicationAssembly);

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);

        return services;
    }
}
