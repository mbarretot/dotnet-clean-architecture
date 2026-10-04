using FluentValidation;

namespace CleanArchitecture.Application.Products.SetProductStock;

public sealed class SetProductStockCommandValidator : AbstractValidator<SetProductStockCommand>
{
    public SetProductStockCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.StockQuantity).GreaterThanOrEqualTo(0);
    }
}
