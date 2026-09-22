using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.SharedKernel.Messaging;

public interface ISender
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    Task<Result> Send(ICommand command, CancellationToken cancellationToken = default);
}
