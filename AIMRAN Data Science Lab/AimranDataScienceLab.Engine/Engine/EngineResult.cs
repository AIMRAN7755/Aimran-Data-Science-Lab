namespace AimranDataScienceLab.Engine;

public enum EngineErrorCode
{
    None = 0,
    ValidationFailed = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    IoFailure = 6,
    ExternalDependencyFailure = 7,
    InternalError = 8
}

public readonly record struct EngineError(EngineErrorCode Code, string Message)
{
    public static EngineError None => new(EngineErrorCode.None, string.Empty);
}

public readonly record struct EngineResult(bool Succeeded, EngineError Error)
{
    public static EngineResult Ok() => new(true, EngineError.None);

    public static EngineResult Fail(EngineErrorCode code, string message)
    {
        if (code == EngineErrorCode.None)
        {
            throw new ArgumentException("Error code must not be None for a failure result.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Error message is required.", nameof(message));
        }

        return new EngineResult(false, new EngineError(code, message));
    }
}

public readonly record struct EngineResult<T>(bool Succeeded, T? Value, EngineError Error)
{
    public static EngineResult<T> Ok(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new EngineResult<T>(true, value, EngineError.None);
    }

    public static EngineResult<T> Fail(EngineErrorCode code, string message) => new(false, default, new EngineError(code, message));
}
