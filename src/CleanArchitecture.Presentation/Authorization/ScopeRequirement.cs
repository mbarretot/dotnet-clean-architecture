using Microsoft.AspNetCore.Authorization;

namespace CleanArchitecture.Presentation.Authorization;

/// <summary>Satisfied by <see cref="ScopeAuthorizationHandler"/> when the user was granted <paramref name="Scope"/>.</summary>
public sealed record ScopeRequirement(string Scope) : IAuthorizationRequirement;
