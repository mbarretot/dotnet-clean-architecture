using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Application.Products.SetProductStock;

public sealed record SetProductStockCommand(Guid Id, int StockQuantity) : ICommand;
