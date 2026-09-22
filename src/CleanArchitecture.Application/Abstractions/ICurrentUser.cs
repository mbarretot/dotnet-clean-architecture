namespace CleanArchitecture.Application.Abstractions;

/// <summary>Implemented by Presentation/Infrastructure, e.g. from the current <c>HttpContext</c> claims.</summary>
public interface ICurrentUser
{
    string? UserId { get; }
}
