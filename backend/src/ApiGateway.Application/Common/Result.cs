namespace ApiGateway.Application.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
    Unauthorized,
}

public sealed record Error(ErrorType Type, string Code, string Message)
{
    public static Error Validation(string code, string message) => new(ErrorType.Validation, code, message);
    public static Error NotFound(string resource) => new(ErrorType.NotFound, "not_found", $"{resource} was not found.");
    public static Error Conflict(string code, string message) => new(ErrorType.Conflict, code, message);
    public static Error Forbidden(string message) => new(ErrorType.Forbidden, "forbidden", message);
    public static Error Unauthorized(string code, string message) => new(ErrorType.Unauthorized, code, message);
}

public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }
    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);

    public static implicit operator Result(Error error) => new(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(null) => _value = value;

    private Result(Error error) : base(error) { }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Result has no value: {Error!.Code}");

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);
}
