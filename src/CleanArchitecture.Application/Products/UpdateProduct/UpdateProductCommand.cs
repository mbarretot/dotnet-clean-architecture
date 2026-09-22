using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Application.Products.UpdateProduct;

public sealed record UpdateProductCommand(Guid Id, string Name, string Description, decimal Price, string Currency) : ICommand;
