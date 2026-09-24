using System.Reflection;
using CleanArchitecture.Application;
using CleanArchitecture.SharedKernel.Messaging;
using Shouldly;

namespace CleanArchitecture.ArchitectureTests;

public class CommandQueryConventionTests
{
    private static readonly Type[] OpenMessageInterfaces =
    [
        typeof(ICommand),
        typeof(ICommand<>),
        typeof(IQuery<>),
    ];

    private static IEnumerable<Type> CommandAndQueryTypes =>
        typeof(DependencyInjection).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetInterfaces()
                .Any(implementedInterface => implementedInterface == typeof(ICommand)
                    || (implementedInterface.IsGenericType && OpenMessageInterfaces.Contains(implementedInterface.GetGenericTypeDefinition()))));

    [Fact]
    public void CommandAndQueryTypes_ShouldExist()
    {
        CommandAndQueryTypes.ShouldNotBeEmpty();
    }

    [Fact]
    public void CommandsAndQueries_ShouldBeSealed()
    {
        var nonSealed = CommandAndQueryTypes.Where(type => !type.IsSealed).ToList();

        nonSealed.ShouldBeEmpty(string.Join(", ", nonSealed.Select(type => type.FullName)));
    }

    [Fact]
    public void CommandsAndQueries_ShouldBeRecords()
    {
        var nonRecords = CommandAndQueryTypes.Where(type => !IsRecord(type)).ToList();

        nonRecords.ShouldBeEmpty(string.Join(", ", nonRecords.Select(type => type.FullName)));
    }

    private static bool IsRecord(Type type) =>
        type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) is not null;
}
