using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CleanArchitecture.IntegrationTests.Infrastructure;

public static class TestJwtTokens
{
    public const string Issuer = "https://integration-tests.cleanarchitecture.local";

    public const string Audience = "cleanarchitecture-api";

    public static readonly string SigningKeyBase64 =
        Convert.ToBase64String(Encoding.UTF8.GetBytes("integration-tests-signing-key-0123456789abcdef"));

    public static string Create(string subject, params string[] scopes) => CreateSignedWith(SigningKeyBase64, subject, scopes);

    public static string CreateSignedWith(string signingKeyBase64, string subject, params string[] scopes)
    {
        var claims = new List<Claim> { new("sub", subject) };

        if (scopes.Length > 0)
        {
            claims.Add(new Claim("scope", string.Join(' ', scopes)));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(signingKeyBase64)),
                SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
