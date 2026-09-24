using CleanArchitecture.SharedKernel.Entities;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Domain.Products;

public sealed class Money : ValueObject
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Result<Money> Create(decimal amount, string? currency)
    {
        if (amount < 0)
        {
            return Result.Failure<Money>(ProductErrors.PriceNegative);
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return Result.Failure<Money>(ProductErrors.CurrencyRequired);
        }

        return Result.Success(new Money(amount, currency.Trim().ToUpperInvariant()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
