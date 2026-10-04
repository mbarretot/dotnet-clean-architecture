using FluentValidation;

namespace CleanArchitecture.Application.Products.GetProducts;

public sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public const int MaxPageSize = 100;

    public const int MaxSearchLength = 200;

    public GetProductsQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(query => query.Search).MaximumLength(MaxSearchLength);
        RuleFor(query => query.MinPrice).GreaterThanOrEqualTo(0);
        RuleFor(query => query.MaxPrice).GreaterThanOrEqualTo(0);
        RuleFor(query => query.MaxPrice)
            .GreaterThanOrEqualTo(query => query.MinPrice)
            .When(query => query.MinPrice is not null && query.MaxPrice is not null);
        RuleFor(query => query.Sort)
            .Must(ProductSortFields.IsValid)
            .WithMessage($"Sort must be one of: {string.Join(", ", ProductSortFields.All)}.");
    }
}
