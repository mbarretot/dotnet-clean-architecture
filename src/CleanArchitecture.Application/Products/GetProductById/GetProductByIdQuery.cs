using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Application.Products.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IQuery<ProductResponse>;
