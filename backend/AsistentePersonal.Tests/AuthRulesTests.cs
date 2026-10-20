using AsistentePersonal.Services;

namespace AsistentePersonal.Tests;

public class AuthRulesTests
{
    [Fact]
    public void ValidateEmail_RejectsBlankEmail()
    {
        Assert.NotNull(AuthRules.ValidateEmail(null));
        Assert.NotNull(AuthRules.ValidateEmail("   "));
    }

    [Fact]
    public void ValidateEmail_RejectsEmailWithoutAtSign()
    {
        Assert.NotNull(AuthRules.ValidateEmail("dueno.ejemplo.test"));
    }

    [Fact]
    public void ValidateEmail_RejectsEmailWithoutDotAfterAtSign()
    {
        Assert.NotNull(AuthRules.ValidateEmail("dueno@ejemplo"));
    }

    [Fact]
    public void ValidateEmail_AcceptsValidEmail_EvenWithSpacesAround()
    {
        Assert.Null(AuthRules.ValidateEmail("  dueno@ejemplo.test  "));
    }

    [Fact]
    public void ValidatePassword_RejectsElevenCharacters()
    {
        Assert.Equal(
            "La contraseña debe tener al menos 12 caracteres.",
            AuthRules.ValidatePassword(new string('a', 11)));
    }

    [Fact]
    public void ValidatePassword_AcceptsTwelveCharacters()
    {
        Assert.Null(AuthRules.ValidatePassword(new string('a', 12)));
    }

    [Fact]
    public void ValidatePassword_RejectsNull()
    {
        Assert.NotNull(AuthRules.ValidatePassword(null));
    }

    [Fact]
    public void ValidateEmail_RejectsMoreThan254Characters()
    {
        var email = new string('a', 245) + "@ejemplo.test"; // 258 characters
        Assert.NotNull(AuthRules.ValidateEmail(email));
    }

    [Fact]
    public void ValidatePassword_RejectsMoreThan256Characters()
    {
        Assert.NotNull(AuthRules.ValidatePassword(new string('a', 257)));
        Assert.Null(AuthRules.ValidatePassword(new string('a', 256)));
    }

    [Fact]
    public void ValidatePasswordConfirmation_RejectsDifferentValues()
    {
        Assert.Equal(
            "Las contraseñas no coinciden.",
            AuthRules.ValidatePasswordConfirmation("una contraseña larga", "otra contraseña larga"));
    }

    [Fact]
    public void ValidatePasswordConfirmation_AcceptsSameValue()
    {
        Assert.Null(AuthRules.ValidatePasswordConfirmation("una contraseña larga", "una contraseña larga"));
    }
}
