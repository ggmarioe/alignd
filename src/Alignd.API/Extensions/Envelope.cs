using Alignd.SharedKernel;

namespace Alignd.API.Extensions;

internal static class Envelope
{
    public static object Ok<T>(T data)              => new { data };
    public static object Empty()                    => new { data = (object?)null };
    public static object Fail(ResultError[] errors) => new { errors };
}
