using AsistentePersonal.Services;

namespace AsistentePersonal.Tests;

public class ReminderSecretTests
{
    [Fact]
    public void Accepts32Characters()
    {
        var secret = new ReminderSecret(new string('s', 32));

        Assert.Equal(new string('s', 32), secret.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void Rejects_ASecretShorterThan32Characters(string value)
    {
        Assert.Throws<ArgumentException>(() => new ReminderSecret(value));
    }

    [Fact]
    public void Rejects_ASecretOf31Characters()
    {
        Assert.Throws<ArgumentException>(() => new ReminderSecret(new string('s', 31)));
    }
}
