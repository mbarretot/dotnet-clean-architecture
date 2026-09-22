using CleanArchitecture.Application;
using CleanArchitecture.SharedKernel.Messaging;
using Shouldly;

namespace CleanArchitecture.ArchitectureTests;

/// <summary>Handlers must be sealed and named "*Handler".</summary>
public class HandlerConventionTests
{
    private static readonly Type[] OpenHandlerInterfaces =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
        typeof(INotificationHandler<>),
    ];

    private static IEnumerable<Type> HandlerTypes =>
        typeof(DependencyInjection).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetInterfaces()
                .Any(implementedInterface => implementedInterface.IsGenericType
                    && OpenHandlerInterfaces.Contains(implementedInterface.GetGenericTypeDefinition())));

    [Fact]
    public void HandlerTypes_ShouldExist()
    {
        HandlerTypes.ShouldNotBeEmpty();
    }

    [Fact]
    public void Handlers_ShouldBeSealed()
    {
        var nonSealed = HandlerTypes.Where(type => !type.IsSealed).ToList();

        nonSealed.ShouldBeEmpty(string.Join(", ", nonSealed.Select(type => type.FullName)));
    }

    [Fact]
    public void Handlers_ShouldBeNamedWithHandlerSuffix()
    {
        var badlyNamed = HandlerTypes.Where(type => !type.Name.EndsWith("Handler", StringComparison.Ordinal)).ToList();

        badlyNamed.ShouldBeEmpty(string.Join(", ", badlyNamed.Select(type => type.FullName)));
    }
}
