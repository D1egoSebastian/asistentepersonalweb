using AsistentePersonal.Models;
using Microsoft.AspNetCore.Identity;

namespace AsistentePersonal.Tests;

// Confirms the framework's password hasher is available and behaves as the plan expects (RNF-1).
public class PasswordHasherTests
{
    [Fact]
    public void Hash_IsNotThePassword_AndVerifiesTheRightOne()
    {
        var hasher = new PasswordHasher<User>();
        var user = new User { Id = User.OwnerId, Email = "dueno@ejemplo.test" };

        var hash = hasher.HashPassword(user, "una contraseña larga");

        Assert.NotEqual("una contraseña larga", hash);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, hash, "una contraseña larga"));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, hash, "otra contraseña larga"));
    }
}
