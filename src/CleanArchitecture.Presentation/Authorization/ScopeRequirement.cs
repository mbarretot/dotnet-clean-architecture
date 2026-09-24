using Microsoft.AspNetCore.Authorization;

namespace CleanArchitecture.Presentation.Authorization;

public sealed record ScopeRequirement(string Scope) : IAuthorizationRequirement;
