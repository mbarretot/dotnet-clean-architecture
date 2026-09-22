using CleanArchitecture.Application.Products.CreateProduct;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Products.CreateProduct;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_HasNoErrors()
    {
        var command = new CreateProductCommand("Keyboard", "Mechanical", 49.99m, "USD", "SKU-1");

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "Description", 10, "USD", "SKU-1")]
    [InlineData("Name", "Description", -1, "USD", "SKU-1")]
    [InlineData("Name", "Description", 10, "", "SKU-1")]
    [InlineData("Name", "Description", 10, "USDX", "SKU-1")]
    [InlineData("Name", "Description", 10, "USD", "")]
    public async Task Validate_WithInvalidCommand_HasErrors(string name, string description, decimal price, string currency, string sku)
    {
        var command = new CreateProductCommand(name, description, price, currency, sku);

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
    }
}
