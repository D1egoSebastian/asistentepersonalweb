using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AsistentePersonal.Services;

// Issues the owner's session tokens (signed JWT). This is the only code that knows the signing key.
public class SessionTokens
{
    public const int MinKeyLength = 32;
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    private const string Issuer = "AsistentePersonal";
    private const string Audience = "AsistentePersonal";

    private readonly SigningCredentials _credentials;

    public SessionTokens(string signingKey)
    {
        if (string.IsNullOrEmpty(signingKey) || signingKey.Length < MinKeyLength)
        {
            throw new ArgumentException($"The signing key must have at least {MinKeyLength} characters.", nameof(signingKey));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        ValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
        };
    }

    // Used by JwtBearer to check incoming tokens, and by the tests.
    public TokenValidationParameters ValidationParameters { get; }

    // A token for the owner, valid for 7 days from "now" (spec 002, CA-2.1 and CA-2.5).
    public string Issue(int ownerId, DateTimeOffset now)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, ownerId.ToString())]),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.Add(Lifetime).UtcDateTime,
            SigningCredentials = _credentials,
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
