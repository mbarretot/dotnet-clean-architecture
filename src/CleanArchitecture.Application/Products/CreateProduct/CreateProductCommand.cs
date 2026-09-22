using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Application.Products.CreateProduct;

public sealed record CreateProductCommand(string Name, string Description, decimal Price, string Currency, string Sku)
    : ICommand<Guid>;
