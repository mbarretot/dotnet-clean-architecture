using CleanArchitecture.Application;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Results;
using NetArchTest.Rules;
using Shouldly;

namespace CleanArchitecture.ArchitectureTests;

/// <summary>The solution ships its own mediator; MediatR must never be referenced, even transitively.</summary>
public class MediatRTests
{
    public static TheoryData<System.Reflection.Assembly> Assemblies => new()
    {
        typeof(Result).Assembly,
        typeof(Product).Assembly,
        typeof(DependencyInjection).Assembly,
        typeof(Infrastructure.DependencyInjection).Assembly,
        typeof(Presentation.Extensions.ResultExtensions).Assembly,
    };

    [Theory]
    [MemberData(nameof(Assemblies))]
    public void Assembly_ShouldNotReferenceMediatR(System.Reflection.Assembly assembly)
    {
        var referencesMediatR = assembly.GetReferencedAssemblies()
            .Any(referenced => referenced.Name?.Equals("MediatR", StringComparison.OrdinalIgnoreCase) == true);

        referencesMediatR.ShouldBeFalse();

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny("MediatR")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }
}
