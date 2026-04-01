using Alignd.SharedKernel;
using Microsoft.AspNetCore.Mvc;

namespace Alignd.API.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result) =>
        result.IsSuccess
            ? result.StatusCode switch
            {
                ResultCode.Created => new ObjectResult(Envelope.Ok(result.Value!)) { StatusCode = 201 },
                _                  => new OkObjectResult(Envelope.Ok(result.Value!))
            }
            : ToErrorResult(result.StatusCode, result.Errors);

    public static IActionResult ToActionResult(this Result result) =>
        result.IsSuccess
            ? result.StatusCode switch
            {
                ResultCode.NoContent => new NoContentResult(),
                _                   => new OkObjectResult(Envelope.Empty())
            }
            : ToErrorResult(result.StatusCode, result.Errors);

    private static IActionResult ToErrorResult(ResultCode code, ResultError[] errors) =>
        new ObjectResult(Envelope.Fail(errors)) { StatusCode = (int)code };
}
