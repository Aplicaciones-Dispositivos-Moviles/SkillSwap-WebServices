namespace SkillSwap.Platform.Shared.Application.Model;

public class Result<T>
{
    protected Result(bool isSuccess, T? value, string message, Enum? error,
        IReadOnlyDictionary<string, object>? details = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Message = message;
        Error = error;
        Details = details;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string Message { get; }
    public Enum? Error { get; }

    /// <summary>
    ///     Optional structured data about a failure, such as the id of the resource that caused a
    ///     conflict. Exposed to clients as extension members of the ProblemDetails response.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Details { get; }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, string.Empty, null);
    }

    public static Result<T> Failure(Enum error, string message)
    {
        return new Result<T>(false, default, message, error);
    }

    public static Result<T> Failure(Enum error, string message, IReadOnlyDictionary<string, object> details)
    {
        return new Result<T>(false, default, message, error, details);
    }
}

public class Result : Result<object>
{
    private Result(bool isSuccess, string message, Enum? error) : base(isSuccess, null, message, error)
    {
    }

    public static Result Success()
    {
        return new Result(true, string.Empty, null);
    }

    public new static Result Failure(Enum error, string message)
    {
        return new Result(false, message, error);
    }
}