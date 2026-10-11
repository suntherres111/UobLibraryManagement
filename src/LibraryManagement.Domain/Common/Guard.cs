namespace LibraryManagement.Domain.Common;

/// <summary>Small helper for enforcing invariants inside entity constructors and methods.</summary>
internal static class Guard
{
    public static string Required(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainValidationException($"{fieldName} must not exceed {maxLength} characters.");
        }

        return trimmed;
    }

    public static string? Optional(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Required(value, fieldName, maxLength);
    }
}
