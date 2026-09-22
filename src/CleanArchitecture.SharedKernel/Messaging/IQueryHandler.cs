using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.SharedKernel.Messaging;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
