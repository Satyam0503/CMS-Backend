using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Utility.Exceptions;

/// <summary>
/// Base for all "expected" application exceptions that should map to a specific
/// HTTP response. Throw one of the derived types from a service and the
/// global ExceptionHandlingMiddleware will translate it into the right
/// status code + JSON body. Anything that doesn't derive from this is treated
/// as an unexpected 500.
/// </summary>
public abstract class AppException : Exception
{
    public int StatusCode { get; }

    protected AppException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    protected AppException(int statusCode, string message, Exception inner) : base(message, inner)
    {
        StatusCode = statusCode;
    }
}

/// <summary>404 — entity does not exist.</summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string message = "Resource not found")
        : base(StatusCodes.Status404NotFound, message) { }
}

/// <summary>400 — bad input from the client.</summary>
public sealed class ValidationException : AppException
{
    public ValidationException(string message)
        : base(StatusCodes.Status400BadRequest, message) { }
}

/// <summary>409 — request conflicts with current state (e.g. duplicate, stale update).</summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string message)
        : base(StatusCodes.Status409Conflict, message) { }
}

/// <summary>403 — authenticated but not allowed.</summary>
public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You do not have permission to perform this action")
        : base(StatusCodes.Status403Forbidden, message) { }
}

/// <summary>401 — not authenticated.</summary>
public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Unauthorized")
        : base(StatusCodes.Status401Unauthorized, message) { }
}

/// <summary>422 — semantically invalid (e.g. business rule violation).</summary>
public sealed class BusinessRuleException : AppException
{
    public BusinessRuleException(string message)
        : base(StatusCodes.Status422UnprocessableEntity, message) { }
}
