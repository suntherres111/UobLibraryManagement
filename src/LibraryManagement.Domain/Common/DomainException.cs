namespace LibraryManagement.Domain.Common;

/// <summary>
/// Raised when a business rule is violated (e.g. book unavailable, loan limit reached).
/// Mapped to HTTP 409 Conflict by the API.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }

    /// <summary>Stable, machine-readable error code for API clients.</summary>
    public string Code { get; }
}

/// <summary>
/// Raised when input data is invalid for the domain (e.g. malformed ISBN).
/// Mapped to HTTP 400 Bad Request by the API.
/// </summary>
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(ErrorCodes.ValidationError, message)
    {
    }
}
