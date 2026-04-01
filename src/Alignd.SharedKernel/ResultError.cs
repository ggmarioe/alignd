namespace Alignd.SharedKernel;

public sealed record ResultError(
    string  Code,
    string  Message,
    string? Field = null
);
