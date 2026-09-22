using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Application.Products.GetProducts;

public sealed record GetProductsQuery(int PageNumber = 1, int PageSize = 20) : IQuery<IReadOnlyList<ProductResponse>>;
