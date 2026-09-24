using FluentValidation;

namespace CleanArchitecture.Application.Orders.PlaceOrder;

public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    /// <summary>Each distinct product is loaded to snapshot it, so the line count is bounded.</summary>
    public const int MaxLines = 100;

    public PlaceOrderCommandValidator()
    {
        RuleFor(command => command.Lines)
            .NotEmpty()
            .Must(lines => lines is null || lines.Count <= MaxLines)
            .WithMessage($"An order cannot contain more than {MaxLines} lines.");

        RuleForEach(command => command.Lines).ChildRules(line =>
        {
            line.RuleFor(orderLine => orderLine.ProductId).NotEmpty();
            line.RuleFor(orderLine => orderLine.Quantity).GreaterThan(0);
        });
    }
}
