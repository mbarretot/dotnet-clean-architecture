using CleanArchitecture.Application.Products.GetProducts;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Products.GetProducts;

public class GetProductsQueryValidatorTests
{
    private readonly GetProductsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithDefaults_IsValid() => _validator.Validate(new GetProductsQuery()).IsValid.ShouldBeTrue();

    [Fact]
    public void Validate_WithEveryFilter_IsValid() =>
        _validator.Validate(new GetProductsQuery(3, 100, "mouse", 0m, 10m, "-price")).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData(0, 20, nameof(GetProductsQuery.PageNumber))]
    [InlineData(1, 0, nameof(GetProductsQuery.PageSize))]
    [InlineData(1, 101, nameof(GetProductsQuery.PageSize))]
    public void Validate_WithPageOutOfRange_IsInvalid(int pageNumber, int pageSize, string property)
    {
        var result = _validator.Validate(new GetProductsQuery(pageNumber, pageSize));

        result.Errors.ShouldContain(error => error.PropertyName == property);
    }

    [Fact]
    public void Validate_WithMinPriceAboveMaxPrice_IsInvalid()
    {
        var result = _validator.Validate(new GetProductsQuery(MinPrice: 20m, MaxPrice: 10m));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(GetProductsQuery.MaxPrice));
    }

    [Theory]
    [InlineData(-1, null, nameof(GetProductsQuery.MinPrice))]
    [InlineData(null, -1, nameof(GetProductsQuery.MaxPrice))]
    public void Validate_WithNegativePrice_IsInvalid(int? minPrice, int? maxPrice, string property)
    {
        var result = _validator.Validate(new GetProductsQuery(MinPrice: minPrice, MaxPrice: maxPrice));

        result.Errors.ShouldContain(error => error.PropertyName == property);
    }

    [Fact]
    public void Validate_WithUnknownSort_NamesTheAllowedValues() =>
        _validator.Validate(new GetProductsQuery(Sort: "stock")).Errors.ShouldHaveSingleItem()
            .ErrorMessage.ShouldBe("Sort must be one of: name, -name, price, -price.");

    [Theory]
    [InlineData("stock")]
    [InlineData("name desc")]
    public void Validate_WithUnknownSort_IsInvalid(string sort)
    {
        var result = _validator.Validate(new GetProductsQuery(Sort: sort));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(GetProductsQuery.Sort));
    }

    [Fact]
    public void Validate_WithTooLongSearch_IsInvalid()
    {
        var result = _validator.Validate(new GetProductsQuery(Search: new string('a', GetProductsQueryValidator.MaxSearchLength + 1)));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(GetProductsQuery.Search));
    }
}
