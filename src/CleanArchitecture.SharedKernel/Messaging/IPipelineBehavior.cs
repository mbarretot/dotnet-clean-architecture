namespace CleanArchitecture.SharedKernel.Messaging;

/// <summary>Invokes the next behavior, or the handler itself when there is none.</summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>Registration order is execution order, outside-in: the first registered runs first, completes last.</summary>
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
