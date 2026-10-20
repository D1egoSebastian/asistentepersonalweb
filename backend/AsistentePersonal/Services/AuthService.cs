using AsistentePersonal.Data;
using AsistentePersonal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AsistentePersonal.Services;

// Use cases for the owner's account. Successful setup and login return a session token.
public class AuthService(AppDbContext db, PasswordHasher<User> hasher, SessionTokens tokens)
{
    public const string WrongCredentialsMessage = "Email o contraseña incorrectos.";
    public const string OwnerExistsMessage = "Ya existe una cuenta.";

    public Task<bool> HasOwnerAsync() => db.Users.AnyAsync();

    public async Task<ServiceResult<string>> SetupAsync(
        string? email, string? password, string? confirmation, DateTimeOffset now)
    {
        if (await HasOwnerAsync())
        {
            return ServiceResult<string>.Conflict(OwnerExistsMessage);
        }

        var error = AuthRules.ValidateEmail(email)
            ?? AuthRules.ValidatePassword(password)
            ?? AuthRules.ValidatePasswordConfirmation(password, confirmation);
        if (error is not null)
        {
            return ServiceResult<string>.Invalid(error);
        }

        var user = new User { Id = User.OwnerId, Email = email!.Trim(), CreatedAt = now };
        user.PasswordHash = hasher.HashPassword(user, password!);
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DatabaseErrors.IsUniqueViolation(ex))
        {
            // Another request created the account first (the owner's Id is always 1).
            db.ChangeTracker.Clear();
            return ServiceResult<string>.Conflict(OwnerExistsMessage);
        }

        return ServiceResult<string>.Ok(tokens.Issue(User.OwnerId, now));
    }

    // Every failure returns the same message, so nobody can learn whether the email exists.
    public async Task<ServiceResult<string>> LoginAsync(string? email, string? password, DateTimeOffset now)
    {
        // The database compares with lower("Email"); the parameter is lowered the same way for every culture.
        var cleanEmail = (email ?? "").Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == cleanEmail);
        if (user is null || password is null)
        {
            return ServiceResult<string>.Unauthorized(WrongCredentialsMessage);
        }

        var check = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (check == PasswordVerificationResult.Failed)
        {
            return ServiceResult<string>.Unauthorized(WrongCredentialsMessage);
        }

        return ServiceResult<string>.Ok(tokens.Issue(user.Id, now));
    }
}
