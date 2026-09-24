using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Domain.Orders;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Abstractions;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Application.Orders.PlaceOrder;

public sealed class PlaceOrderCommandHandler(
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<PlaceOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } customerId)
        {
            return Result.Failure<Guid>(OrderErrors.CustomerRequired);
        }

        var products = new Dictionary<Guid, Product>();
        foreach (var productId in request.Lines.Select(line => line.ProductId).Distinct())
        {
            var product = await productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
            if (product is null)
            {
                return Result.Failure<Guid>(OrderErrors.ProductNotFound(productId));
            }

            products.Add(productId, product);
        }

        var drafts = request.Lines
            .Select(line =>
            {
                var product = products[line.ProductId];
                return new OrderLineDraft(product.Id, product.Name, product.Price, line.Quantity);
            })
            .ToList();

        var orderResult = Order.Place(customerId, drafts);
        if (orderResult.IsFailure)
        {
            return Result.Failure<Guid>(orderResult.Error);
        }

        var order = orderResult.Value;
        order.CreatedAt = dateTimeProvider.UtcNow;

        orderRepository.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return order.Id;
    }
}
