using AsistentePersonal.Data;
using AsistentePersonal.Models;
using AsistentePersonal.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace AsistentePersonal.Tests;

public class AuthServiceTests
{
    // Test-only values. They never leave the test database.
    private const string Key = "clave-de-prueba-de-al-menos-32-caracteres";
    private const string Email = "dueno@ejemplo.test";
    private const string Password = "contraseña de prueba larga";

    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static AuthService CreateService(AppDbContext db) =>
        new(db, new PasswordHasher<User>(), new SessionTokens(Key));

    // ---- T06: status and setup ----

    [Fact]
    public async Task HasOwner_IsFalse_WithoutAccount()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();

        Assert.False(await CreateService(db).HasOwnerAsync());
    }

    [Fact]
    public async Task Setup_CreatesTheAccount_StoresOnlyAHash_AndReturnsAValidSession()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = CreateService(db);

        var result = await service.SetupAsync(Email, Password, Password, Now);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.True(await service.HasOwnerAsync());
        var saved = await db.Users.SingleAsync();
        Assert.Equal(User.OwnerId, saved.Id);
        Assert.NotEqual(Password, saved.PasswordHash);
        var validation = await new JsonWebTokenHandler()
            .ValidateTokenAsync(result.Value!, new SessionTokens(Key).ValidationParameters);
        Assert.True(validation.IsValid);
    }

    [Fact]
    public async Task Setup_WhenAnAccountExists_ReturnsConflict()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = CreateService(db);
        await service.SetupAsync(Email, Password, Password, Now);

        var second = await service.SetupAsync("otro@ejemplo.test", Password, Password, Now);

        Assert.Equal(ResultStatus.Conflict, second.Status);
        Assert.Equal(1, await db.Users.CountAsync());
    }

    [Fact]
    public async Task Setup_TwoAttemptsAtTheSameTime_CreateOnlyOneAccount()
    {
        await using var first = await TestDatabase.CreateEmptyContextAsync();
        await using var second = await TestDatabase.OpenAsync();

        var results = await Task.WhenAll(
            CreateService(first).SetupAsync(Email, Password, Password, Now),
            CreateService(second).SetupAsync("otro@ejemplo.test", Password, Password, Now));

        Assert.Single(results, r => r.Status == ResultStatus.Ok);
        Assert.Single(results, r => r.Status == ResultStatus.Conflict);
        await using var check = await TestDatabase.OpenAsync();
        Assert.Equal(1, await check.Users.CountAsync());
    }

    [Fact]
    public async Task Setup_WithInvalidData_ReturnsInvalid_AndCreatesNothing()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = CreateService(db);

        Assert.Equal(ResultStatus.Invalid, (await service.SetupAsync("sin-arroba", Password, Password, Now)).Status);
        Assert.Equal(ResultStatus.Invalid, (await service.SetupAsync(Email, "corta", "corta", Now)).Status);
        Assert.Equal(ResultStatus.Invalid, (await service.SetupAsync(Email, Password, "distinta pero larga", Now)).Status);
        Assert.False(await service.HasOwnerAsync());
    }

    // ---- T07: login ----

    [Fact]
    public async Task Login_WithTheRightCredentials_ReturnsASession()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = CreateService(db);
        await service.SetupAsync(Email, Password, Password, Now);

        var result = await service.LoginAsync(Email, Password, Now);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.False(string.IsNullOrEmpty(result.Value));
    }

    [Fact]
    public async Task Login_IgnoresTheCaseOfTheEmail()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = CreateService(db);
        await service.SetupAsync(Email, Password, Password, Now);

        var result = await service.LoginAsync("  DUENO@Ejemplo.Test ", Password, Now);

        Assert.Equal(ResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Login_WithWrongPassword_UnknownEmail_OrNoAccount_ReturnsTheSameMessage()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = CreateService(db);

        var noAccount = await service.LoginAsync(Email, Password, Now);
        await service.SetupAsync(Email, Password, Password, Now);
        var wrongPassword = await service.LoginAsync(Email, "otra contraseña larga", Now);
        var unknownEmail = await service.LoginAsync("otro@ejemplo.test", Password, Now);

        foreach (var result in new[] { noAccount, wrongPassword, unknownEmail })
        {
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Equal("Email o contraseña incorrectos.", result.Message);
        }
    }
}
