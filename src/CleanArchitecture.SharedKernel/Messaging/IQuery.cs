using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.SharedKernel.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
