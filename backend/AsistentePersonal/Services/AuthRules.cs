using System.Text.RegularExpressions;

namespace AsistentePersonal.Services;

// Pure rules for the owner's account. Each method returns null when the input is valid,
// or a message in Spanish when it is not.
public static partial class AuthRules
{
    public const int MinPasswordLength = 12;

    // Upper limits keep anonymous requests from sending huge values to hash or compare.
    public const int MaxPasswordLength = 256;
    public const int MaxEmailLength = 254;

    public static string? ValidateEmail(string? email)
    {
        if (HabitRules.IsBlank(email))
        {
            return "El email es obligatorio.";
        }

        var clean = email!.Trim();
        if (clean.Length > MaxEmailLength)
        {
            return $"El email no puede tener más de {MaxEmailLength} caracteres.";
        }

        return EmailPattern().IsMatch(clean) ? null : "El email no es válido.";
    }

    // Length counts what the user sees as characters, like habit names (an emoji is 1).
    public static string? ValidatePassword(string? password)
    {
        if (password is null || HabitRules.CountChars(password) < MinPasswordLength)
        {
            return $"La contraseña debe tener al menos {MinPasswordLength} caracteres.";
        }

        if (HabitRules.CountChars(password) > MaxPasswordLength)
        {
            return $"La contraseña no puede tener más de {MaxPasswordLength} caracteres.";
        }

        return null;
    }

    public static string? ValidatePasswordConfirmation(string? password, string? confirmation)
    {
        return password == confirmation ? null : "Las contraseñas no coinciden.";
    }

    // Something before the @, something after it, and a dot in the part after it.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
