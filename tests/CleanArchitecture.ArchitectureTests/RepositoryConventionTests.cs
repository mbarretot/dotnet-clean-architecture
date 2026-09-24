using CleanArchitecture.Domain.Products;
using CleanArchitecture.Infrastructure;
using Shouldly;

namespace CleanArchitecture.ArchitectureTests;

public class RepositoryConventionTests
{
    private static readonly System.Reflection.Assembly DomainAssembly = typeof(IProductRepository).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(DependencyInjection).Assembly;

    private static IEnumerable<Type> RepositoryInterfaces =>
        DomainAssembly.GetTypes().Where(type => type is { IsInterface: true } && type.Name.EndsWith("Repository", StringComparison.Ordinal));

    private static IEnumerable<Type> RepositoryImplementations =>
        InfrastructureAssembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => RepositoryInterfaces.Any(repositoryInterface => repositoryInterface.IsAssignableFrom(type)));

    [Fact]
    public void RepositoryInterfaces_ShouldExist()
    {
        RepositoryInterfaces.ShouldNotBeEmpty();
    }

    [Fact]
    public void RepositoryImplementations_ShouldExistInInfrastructure()
    {
        RepositoryImplementations.ShouldNotBeEmpty();
    }

    [Fact]
    public void RepositoryImplementations_ShouldBeSealed()
    {
        var nonSealed = RepositoryImplementations.Where(type => !type.IsSealed).ToList();

        nonSealed.ShouldBeEmpty(string.Join(", ", nonSealed.Select(type => type.FullName)));
    }

    [Fact]
    public void RepositoryImplementations_ShouldLiveInInfrastructureNamespace()
    {
        var misplaced = RepositoryImplementations
            .Where(type => type.Namespace is null || !type.Namespace.StartsWith("CleanArchitecture.Infrastructure", StringComparison.Ordinal))
            .ToList();

        misplaced.ShouldBeEmpty(string.Join(", ", misplaced.Select(type => type.FullName)));
    }
}
