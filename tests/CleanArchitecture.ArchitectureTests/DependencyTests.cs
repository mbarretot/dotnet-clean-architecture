using CleanArchitecture.Application;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Results;
using NetArchTest.Rules;
using Shouldly;

namespace CleanArchitecture.ArchitectureTests;

/// <summary>Enforces inward-only dependencies between layers.</summary>
public class DependencyTests
{
    private static readonly System.Reflection.Assembly SharedKernelAssembly = typeof(Result).Assembly;
    private static readonly System.Reflection.Assembly DomainAssembly = typeof(Product).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(DependencyInjection).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly =
        typeof(Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void SharedKernel_ShouldNotDependOnDomainOrApplicationOrInfrastructureOrPresentation()
    {
        var result = Types.InAssembly(SharedKernelAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "CleanArchitecture.Domain",
                "CleanArchitecture.Application",
                "CleanArchitecture.Infrastructure",
                "CleanArchitecture.Presentation")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureMessage(result));
    }

    [Fact]
    public void Domain_ShouldNotDependOnApplicationOrInfrastructureOrPresentation()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "CleanArchitecture.Application",
                "CleanArchitecture.Infrastructure",
                "CleanArchitecture.Presentation")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureMessage(result));
    }

    [Fact]
    public void Domain_ShouldOnlyDependOnSharedKernelAndSystem()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("CleanArchitecture.Domain")
            .Should()
            .NotHaveDependencyOnAny(
                "CleanArchitecture.Application",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureMessage(result));
    }

    [Fact]
    public void Application_ShouldNotDependOnInfrastructureOrPresentation()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "CleanArchitecture.Infrastructure",
                "CleanArchitecture.Presentation",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureMessage(result));
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnPresentation()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOnAny("CleanArchitecture.Presentation")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureMessage(result));
    }

    private static string FailureMessage(NetArchTest.Rules.TestResult result) =>
        $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}";
}
