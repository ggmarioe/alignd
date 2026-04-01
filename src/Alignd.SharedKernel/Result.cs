namespace Alignd.SharedKernel;

/// <summary>
/// Result with a return value — use for handlers that produce data.
/// </summary>
public sealed class Result<T>
{
    public bool          IsSuccess  { get; }
    public bool          IsFailure  => !IsSuccess;
    public ResultCode    StatusCode { get; }
    public T?            Value      { get; }
    public ResultError[] Errors     { get; }

    private Result(bool isSuccess, ResultCode code, T? value, ResultError[] errors)
    {
        IsSuccess  = isSuccess;
        StatusCode = code;
        Value      = value;
        Errors     = errors;
    }

    public static Result<T> Ok(T value)      => new(true, ResultCode.Ok,      value,   []);
    public static Result<T> Created(T value) => new(true, ResultCode.Created, value,   []);

    public static Result<T> NotFound(string code, string message) =>
        new(false, ResultCode.NotFound, default, [new ResultError(code, message)]);

    public static Result<T> Conflict(string code, string message) =>
        new(false, ResultCode.Conflict, default, [new ResultError(code, message)]);

    public static Result<T> BadRequest(string code, string message) =>
        new(false, ResultCode.BadRequest, default, [new ResultError(code, message)]);

    public static Result<T> Unauthorized(string code, string message) =>
        new(false, ResultCode.Unauthorized, default, [new ResultError(code, message)]);

    public static Result<T> Forbidden(string code, string message) =>
        new(false, ResultCode.Forbidden, default, [new ResultError(code, message)]);

    public static Result<T> Unprocessable(IEnumerable<ResultError> errors) =>
        new(false, ResultCode.Unprocessable, default, [.. errors]);

    public static Result<T> Unprocessable(string code, string message, string? field = null) =>
        new(false, ResultCode.Unprocessable, default, [new ResultError(code, message, field)]);

    public static implicit operator Result<T>(T value) => Ok(value);
}

/// <summary>
/// Result without a return value — use for void operations.
/// </summary>
public sealed class Result
{
    public bool          IsSuccess  { get; }
    public bool          IsFailure  => !IsSuccess;
    public ResultCode    StatusCode { get; }
    public ResultError[] Errors     { get; }

    private Result(bool isSuccess, ResultCode code, ResultError[] errors)
    {
        IsSuccess  = isSuccess;
        StatusCode = code;
        Errors     = errors;
    }

    public static Result Ok()        => new(true, ResultCode.Ok,        []);
    public static Result NoContent() => new(true, ResultCode.NoContent, []);

    public static Result NotFound(string code, string message) =>
        new(false, ResultCode.NotFound, [new ResultError(code, message)]);

    public static Result Conflict(string code, string message) =>
        new(false, ResultCode.Conflict, [new ResultError(code, message)]);

    public static Result BadRequest(string code, string message) =>
        new(false, ResultCode.BadRequest, [new ResultError(code, message)]);

    public static Result Unauthorized(string code, string message) =>
        new(false, ResultCode.Unauthorized, [new ResultError(code, message)]);

    public static Result Forbidden(string code, string message) =>
        new(false, ResultCode.Forbidden, [new ResultError(code, message)]);

    public static Result Unprocessable(IEnumerable<ResultError> errors) =>
        new(false, ResultCode.Unprocessable, [.. errors]);

    public static Result Unprocessable(string code, string message, string? field = null) =>
        new(false, ResultCode.Unprocessable, [new ResultError(code, message, field)]);
}
