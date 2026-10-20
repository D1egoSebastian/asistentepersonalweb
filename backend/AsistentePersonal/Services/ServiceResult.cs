namespace AsistentePersonal.Services;

public enum ResultStatus
{
    Ok,
    Invalid,
    NotFound,
    Conflict,
    Unauthorized
}

// Outcome of a use case with no value to return. The controller maps Status to HTTP (plan §5.3).
public sealed record ServiceResult(ResultStatus Status, string? Message = null)
{
    public static ServiceResult Ok() => new(ResultStatus.Ok);

    public static ServiceResult Invalid(string message) => new(ResultStatus.Invalid, message);

    public static ServiceResult NotFound() => new(ResultStatus.NotFound, HabitService.NotFoundMessage);
}

// Outcome of a use case that returns a value when it succeeds.
public sealed record ServiceResult<T>(ResultStatus Status, T? Value = default, string? Message = null)
{
    public static ServiceResult<T> Ok(T value) => new(ResultStatus.Ok, value);

    public static ServiceResult<T> Invalid(string message) => new(ResultStatus.Invalid, default, message);

    public static ServiceResult<T> NotFound() => new(ResultStatus.NotFound, default, HabitService.NotFoundMessage);

    public static ServiceResult<T> Conflict(string message) => new(ResultStatus.Conflict, default, message);

    public static ServiceResult<T> Unauthorized(string message) => new(ResultStatus.Unauthorized, default, message);
}
