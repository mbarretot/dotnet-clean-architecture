namespace CleanArchitecture.Application.Abstractions;

public interface ICurrentUser
{
    string? UserId { get; }
}
