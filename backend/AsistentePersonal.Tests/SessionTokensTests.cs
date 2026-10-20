using System.Security.Claims;
using AsistentePersonal.Services;
using Microsoft.IdentityModel.JsonWebTokens;

namespace AsistentePersonal.Tests;

public class SessionTokensTests
{
    // Test-only keys. Real keys come from the Jwt__SigningKey environment variable.
    private const string Key = "clave-de-prueba-de-al-menos-32-caracteres";
    private const string OtherKey = "otra-clave-de-prueba-de-al-menos-32-caracteres";

    [Fact]
    public async Task IssuedToken_IsValid_WithTheSameKey_AndCarriesTheOwnerId()
    {
        var tokens = new SessionTokens(Key);

        var token = tokens.Issue(1, DateTimeOffset.UtcNow);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, tokens.ValidationParameters);

        Assert.True(result.IsValid);
        Assert.Equal("1", result.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    [Fact]
    public async Task IssuedToken_IsInvalid_WithAnotherKey()
    {
        var token = new SessionTokens(Key).Issue(1, DateTimeOffset.UtcNow);

        var result = await new JsonWebTokenHandler()
            .ValidateTokenAsync(token, new SessionTokens(OtherKey).ValidationParameters);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task IssuedToken_IsInvalid_AfterSevenDays()
    {
        var tokens = new SessionTokens(Key);

        var token = tokens.Issue(1, DateTimeOffset.UtcNow.AddDays(-8));
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, tokens.ValidationParameters);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void IssuedToken_ExpiresSevenDaysAfterIssue()
    {
        var now = new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

        var token = new JsonWebToken(new SessionTokens(Key).Issue(1, now));

        Assert.Equal(now.AddDays(7).UtcDateTime, token.ValidTo);
    }

    [Fact]
    public void Constructor_RejectsKeysShorterThan32Characters()
    {
        Assert.Throws<ArgumentException>(() => new SessionTokens("corta"));
    }
}
