using CleanArchitecture.Application.Orders.PlaceOrder;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Orders.PlaceOrder;

public class PlaceOrderCommandValidatorTests
{
    private readonly PlaceOrderCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_HasNoErrors()
    {
        var command = new PlaceOrderCommand([new PlaceOrderLine(Guid.NewGuid(), 1), new PlaceOrderLine(Guid.NewGuid(), 3)]);

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_WithoutLines_HasErrors()
    {
        var result = await _validator.ValidateAsync(new PlaceOrderCommand([]), TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Validate_WithEmptyProductId_HasErrors()
    {
        var command = new PlaceOrderCommand([new PlaceOrderLine(Guid.Empty, 1)]);

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validate_WithNonPositiveQuantity_HasErrors(int quantity)
    {
        var command = new PlaceOrderCommand([new PlaceOrderLine(Guid.NewGuid(), quantity)]);

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
    }
}
