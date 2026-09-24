using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Application.Orders.GetMyOrders;
using CleanArchitecture.Domain.Orders;
using NSubstitute;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Orders.GetMyOrders;

public class GetMyOrdersQueryHandlerTests
{
    private const string CustomerId = "customer-1";

    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetMyOrdersQueryHandler CreateSut() => new(_orderRepository, _currentUser);

    [Fact]
    public async Task Handle_ReturnsTheCurrentCustomersOrdersForTheRequestedPage()
    {
        _currentUser.UserId.Returns(CustomerId);
        var first = OrderTestData.PlacedOrder(CustomerId);
        var second = OrderTestData.PlacedOrder(CustomerId);
        _orderRepository.GetByCustomerAsync(CustomerId, 2, 5, Arg.Any<CancellationToken>()).Returns([first, second]);

        var result = await CreateSut().Handle(new GetMyOrdersQuery(2, 5), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(order => order.Id).ShouldBe([first.Id, second.Id]);
    }

    [Fact]
    public async Task Handle_WithoutCurrentUser_ReturnsEmptyListWithoutQuerying()
    {
        _currentUser.UserId.Returns((string?)null);

        var result = await CreateSut().Handle(new GetMyOrdersQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
        await _orderRepository.DidNotReceive().GetByCustomerAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
