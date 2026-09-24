using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Domain.Orders;

public static class OrderErrors
{
    public static readonly Error CustomerRequired =
        Error.Validation("Order.CustomerRequired", "An order must belong to a customer.");

    public static readonly Error NoLines =
        Error.Validation("Order.NoLines", "An order must contain at least one line.");

    public static readonly Error QuantityNotPositive =
        Error.Validation("Order.QuantityNotPositive", "The quantity of an order line must be greater than zero.");

    public static readonly Error CurrencyMismatch =
        Error.Validation("Order.CurrencyMismatch", "All lines of an order must use the same currency.");

    public static readonly Error AlreadyCancelled =
        Error.Conflict("Order.AlreadyCancelled", "The order has already been cancelled.");

    public static Error NotFound(Guid orderId) =>
        Error.NotFound("Order.NotFound", $"The order with identifier '{orderId}' was not found.");

    public static Error ProductNotFound(Guid productId) =>
        Error.NotFound("Order.ProductNotFound", $"The product with identifier '{productId}' was not found.");
}
