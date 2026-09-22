using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.SharedKernel.Messaging;

/// <summary>A read-only request.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
