using System.Reflection;
using CleanArchitecture.Domain.Orders;
using CleanArchitecture.SharedKernel.Entities;
using Shouldly;

namespace CleanArchitecture.ArchitectureTests;

/// <summary>
/// Aggregates reference each other by id only: no Domain entity may hold another aggregate root (or a collection of
/// them) as a property. A navigation across the boundary would let one transaction load and modify two aggregates.
/// </summary>
public class AggregateBoundaryTests
{
    private static readonly Assembly DomainAssembly = typeof(Order).Assembly;

    private static IEnumerable<Type> EntityTypes =>
        DomainAssembly.GetTypes().Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(BaseEntity)));

    [Fact]
    public void AggregateRoots_ShouldExist()
    {
        EntityTypes.Count(type => type.IsAssignableTo(typeof(AggregateRoot))).ShouldBeGreaterThan(1);
    }

    [Fact]
    public void Entities_ShouldNotReferenceOtherAggregateRootsByNavigation()
    {
        var violations = EntityTypes
            .SelectMany(entityType => entityType
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(property => ReferencedAggregateRoot(property.PropertyType) is { } referenced && referenced != entityType)
                .Select(property => $"{entityType.Name}.{property.Name}"))
            .ToList();

        violations.ShouldBeEmpty(string.Join(", ", violations));
    }

    /// <summary>The aggregate root type <paramref name="type"/> is, or is a sequence of; otherwise null.</summary>
    private static Type? ReferencedAggregateRoot(Type type)
    {
        if (type.IsAssignableTo(typeof(AggregateRoot)))
        {
            return type;
        }

        var elementType = type.IsArray
            ? type.GetElementType()
            : type.GetInterfaces().Append(type)
                .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                ?.GetGenericArguments()[0];

        return elementType?.IsAssignableTo(typeof(AggregateRoot)) == true ? elementType : null;
    }
}
