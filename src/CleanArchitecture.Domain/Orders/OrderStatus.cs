namespace CleanArchitecture.Domain.Orders;

public enum OrderStatus
{
    Placed = 0,
    Cancelled = 1,
    Paid = 2,
    Shipped = 3,
    Completed = 4,
}
