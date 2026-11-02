namespace AsistentePersonal.Services;

// The value the scheduled flow must send in X-Reminder-Secret (environment variable Reminders__Secret).
// Like the signing key, it needs at least 32 characters, so a weak secret stops the app at startup.
public sealed class ReminderSecret
{
    public const int MinLength = 32;

    public ReminderSecret(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < MinLength)
        {
            throw new ArgumentException($"The reminders secret must have at least {MinLength} characters.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }
}
