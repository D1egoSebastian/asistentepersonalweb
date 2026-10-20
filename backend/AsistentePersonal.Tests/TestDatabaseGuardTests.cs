namespace AsistentePersonal.Tests;

public class TestDatabaseGuardTests
{
    [Fact]
    public void RefusesADatabaseThatIsNotATestOne()
    {
        Assert.Throws<InvalidOperationException>(() =>
            TestDatabase.EnsureIsTestDatabase("Host=localhost;Database=asistente_dev;Username=postgres"));
    }

    [Fact]
    public void AcceptsADatabaseWhoseNameEndsWithTest()
    {
        TestDatabase.EnsureIsTestDatabase("Host=localhost;Database=asistente_test;Username=postgres");
    }
}
