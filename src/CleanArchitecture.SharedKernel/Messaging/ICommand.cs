using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.SharedKernel.Messaging;

/// <summary>A state-mutating request.</summary>
public interface ICommand : IRequest<Result>;

/// <summary>A state-mutating request that returns a value.</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
