using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Entities;

namespace CleanArchitecture.Domain.Orders;

/// <summary>
/// Entity inside the <see cref="Order"/> aggregate, changed only through it. References the product by id, never by
/// navigation: <see cref="ProductName"/> and <see cref="UnitPrice"/> are snapshots taken when the order was placed.
/// </summary>
public sealed class OrderLine : BaseEntity
{
    private OrderLine(Guid id, Guid productId, string productName, Money unitPrice, int quantity)
        : base(id)
    {
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    /// <summary>For EF Core only: <see cref="UnitPrice"/> is an owned-type navigation, which constructor binding can't populate.</summary>
    private OrderLine()
    {
        ProductName = string.Empty;
        UnitPrice = null!;
    }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; }

    public Money UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public Money LineTotal => Money.Create(UnitPrice.Amount * Quantity, UnitPrice.Currency).Value;

    internal static OrderLine Create(OrderLineDraft draft) =>
        new(Guid.NewGuid(), draft.ProductId, draft.ProductName.Trim(), draft.UnitPrice, draft.Quantity);

    internal void IncreaseQuantity(int quantity) => Quantity += quantity;
}
