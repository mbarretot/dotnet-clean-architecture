using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.SharedKernel.Messaging;

public static class MediatorServiceCollectionExtensions
{
    private static readonly Type[] OpenHandlerInterfaces =
    [
        typeof(IRequestHandler<,>),
        typeof(INotificationHandler<>),
    ];

    public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        services.AddScoped<ISender, Sender>();
        services.AddScoped<IPublisher, Publisher>();

        foreach (var assembly in assemblies)
        {
            RegisterHandlers(services, assembly);
        }

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var candidateTypes = assembly.GetTypes().Where(type => type is { IsClass: true, IsAbstract: false });

        foreach (var type in candidateTypes)
        {
            var handlerInterfaces = type.GetInterfaces()
                .Where(implementedInterface => implementedInterface.IsGenericType
                    && OpenHandlerInterfaces.Contains(implementedInterface.GetGenericTypeDefinition()));

            foreach (var handlerInterface in handlerInterfaces)
            {
                services.AddScoped(handlerInterface, type);
            }
        }
    }
}
