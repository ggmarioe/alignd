using Alignd.SharedKernel;

namespace Alignd.Tests.SharedKernel;

[TestFixture]
public sealed class ResultTests
{
    // ─── Result<T> ───────────────────────────────────────────────────────────

    [Test]
    public void GenericResult_Ok_IsSuccessful_AndCarriesValue()
    {
        var result = Result<string>.Ok("hello");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess,  Is.True);
            Assert.That(result.IsFailure,  Is.False);
            Assert.That(result.Value,      Is.EqualTo("hello"));
            Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Ok));
            Assert.That(result.Errors,     Is.Empty);
        });
    }

    [Test]
    public void GenericResult_Created_IsSuccessful_WithCreatedStatusCode()
    {
        var result = Result<int>.Created(42);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess,  Is.True);
            Assert.That(result.Value,      Is.EqualTo(42));
            Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Created));
        });
    }

    [Test]
    public void GenericResult_NotFound_IsFailure_WithNotFoundStatusCode_AndErrorMessage()
    {
        var result = Result<string>.NotFound("item.not_found", "Item does not exist.");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure,           Is.True);
            Assert.That(result.StatusCode,          Is.EqualTo(ResultCode.NotFound));
            Assert.That(result.Value,               Is.Null);
            Assert.That(result.Errors,              Has.Length.EqualTo(1));
            Assert.That(result.Errors[0].Code,      Is.EqualTo("item.not_found"));
            Assert.That(result.Errors[0].Message,   Is.EqualTo("Item does not exist."));
        });
    }

    [Test]
    public void GenericResult_Conflict_IsFailure_WithConflictStatusCode()
    {
        var result = Result<string>.Conflict("item.conflict", "Already exists.");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure,  Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Conflict));
        });
    }

    [Test]
    public void GenericResult_Forbidden_IsFailure_WithForbiddenStatusCode()
    {
        var result = Result<string>.Forbidden("access.denied", "Not allowed.");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure,  Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Forbidden));
        });
    }

    [Test]
    public void GenericResult_Unauthorized_IsFailure_WithUnauthorizedStatusCode()
    {
        var result = Result<string>.Unauthorized("auth.required", "Please log in.");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure,  Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Unauthorized));
        });
    }

    [Test]
    public void GenericResult_BadRequest_IsFailure_WithBadRequestStatusCode()
    {
        var result = Result<string>.BadRequest("bad.input", "Invalid data.");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure,  Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(ResultCode.BadRequest));
        });
    }

    [Test]
    public void GenericResult_Unprocessable_WithFieldProvided_SetsFieldOnError()
    {
        var result = Result<string>.Unprocessable("field.invalid", "Too short.", "username");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure,            Is.True);
            Assert.That(result.StatusCode,           Is.EqualTo(ResultCode.Unprocessable));
            Assert.That(result.Errors[0].Field,      Is.EqualTo("username"));
        });
    }

    [Test]
    public void GenericResult_Unprocessable_FromMultipleErrors_ContainsAllErrors()
    {
        var errors = new[]
        {
            new ResultError("e1", "Error one"),
            new ResultError("e2", "Error two")
        };

        var result = Result<string>.Unprocessable(errors);

        Assert.That(result.Errors, Has.Length.EqualTo(2));
    }

    [Test]
    public void GenericResult_ImplicitConversion_FromValue_ProducesOkResult()
    {
        Result<string> result = "implicit value";

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value,     Is.EqualTo("implicit value"));
        });
    }

    // ─── Result (non-generic) ────────────────────────────────────────────────

    [Test]
    public void Result_Ok_IsSuccessful_WithOkStatusCode()
    {
        var result = Result.Ok();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess,  Is.True);
            Assert.That(result.IsFailure,  Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Ok));
            Assert.That(result.Errors,     Is.Empty);
        });
    }

    [Test]
    public void Result_NoContent_IsSuccessful_WithNoContentStatusCode()
    {
        var result = Result.NoContent();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess,  Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(ResultCode.NoContent));
        });
    }

    [Test]
    public void Result_NotFound_IsFailure_WithCorrectErrorCode()
    {
        var result = Result.NotFound("room.not_found", "Room not found.");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure,          Is.True);
            Assert.That(result.StatusCode,         Is.EqualTo(ResultCode.NotFound));
            Assert.That(result.Errors[0].Code,     Is.EqualTo("room.not_found"));
        });
    }

    [Test]
    public void Result_Conflict_IsFailure_WithConflictStatusCode()
    {
        var result = Result.Conflict("already.exists", "Duplicate.");

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Conflict));
    }

    [Test]
    public void Result_Forbidden_IsFailure_WithForbiddenStatusCode()
    {
        var result = Result.Forbidden("not.admin", "Admins only.");

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Forbidden));
    }

    [Test]
    public void Result_Unauthorized_IsFailure_WithUnauthorizedStatusCode()
    {
        var result = Result.Unauthorized("token.invalid", "Token expired.");

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Unauthorized));
    }

    [Test]
    public void Result_BadRequest_IsFailure_WithBadRequestStatusCode()
    {
        var result = Result.BadRequest("bad.data", "Invalid.");

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.BadRequest));
    }

    [Test]
    public void Result_Unprocessable_IsFailure_WithFieldOnError()
    {
        var result = Result.Unprocessable("field.required", "Required.", "email");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure,        Is.True);
            Assert.That(result.StatusCode,       Is.EqualTo(ResultCode.Unprocessable));
            Assert.That(result.Errors[0].Field,  Is.EqualTo("email"));
        });
    }

    [Test]
    public void Result_Unprocessable_FromMultipleErrors_ContainsAllErrors()
    {
        var errors = new[]
        {
            new ResultError("e1", "Error one"),
            new ResultError("e2", "Error two"),
            new ResultError("e3", "Error three")
        };

        var result = Result.Unprocessable(errors);

        Assert.That(result.Errors, Has.Length.EqualTo(3));
    }
}
