using CleanArchitecture.Domain.Products;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Domain.Products;

public class MoneyAndSkuTests
{
    [Fact]
    public void Money_WithZeroAmount_IsValid() => Money.Create(0m, "usd").Value.ShouldBe(Money.Create(0m, "USD").Value);

    [Fact]
    public void Money_WithNegativeAmount_Fails() => Money.Create(-0.01m, "USD").Error.ShouldBe(ProductErrors.PriceNegative);

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void Money_WithoutCurrency_Fails(string? currency) =>
        Money.Create(1m, currency).Error.ShouldBe(ProductErrors.CurrencyRequired);

    [Fact]
    public void Money_EqualityUsesAmountAndCurrency()
    {
        var tenUsd = Money.Create(10m, "USD").Value;

        tenUsd.ShouldNotBe(Money.Create(11m, "USD").Value);
        tenUsd.ShouldNotBe(Money.Create(10m, "EUR").Value);
    }

    [Fact]
    public void Sku_IsTrimmedUpperCasedAndComparedByValue()
    {
        var sku = Sku.Create(" kb-1 ").Value;

        sku.Value.ShouldBe("KB-1");
        sku.ToString().ShouldBe("KB-1");
        sku.ShouldBe(Sku.Create("KB-1").Value);
        sku.ShouldNotBe(Sku.Create("KB-2").Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Sku_WithoutValue_Fails(string? value) => Sku.Create(value).Error.ShouldBe(ProductErrors.SkuRequired);
}
