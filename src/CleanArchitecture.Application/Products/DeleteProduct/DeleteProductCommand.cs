using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Application.Products.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : ICommand;
