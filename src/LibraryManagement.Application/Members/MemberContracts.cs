using System.ComponentModel.DataAnnotations;
using LibraryManagement.Application.Common;
using LibraryManagement.Domain.Members;

namespace LibraryManagement.Application.Members;

public sealed record MemberResponse(
    int Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? PhoneNumber,
    MembershipType MembershipType,
    int MaxActiveLoans,
    int LoanPeriodDays,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed class CreateMemberRequest
{
    /// <example>Siti</example>
    [Required, StringLength(Member.NameMaxLength)]
    public string FirstName { get; init; } = string.Empty;

    /// <example>Aminah</example>
    [Required, StringLength(Member.NameMaxLength)]
    public string LastName { get; init; } = string.Empty;

    /// <example>siti.aminah@example.com</example>
    [Required, EmailAddress, StringLength(Member.EmailMaxLength)]
    public string Email { get; init; } = string.Empty;

    /// <example>+60123456789</example>
    [Phone, StringLength(Member.PhoneMaxLength)]
    public string? PhoneNumber { get; init; }

    public MembershipType MembershipType { get; init; } = MembershipType.Standard;
}

public sealed class UpdateMemberRequest
{
    [Required, StringLength(Member.NameMaxLength)]
    public string FirstName { get; init; } = string.Empty;

    [Required, StringLength(Member.NameMaxLength)]
    public string LastName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(Member.EmailMaxLength)]
    public string Email { get; init; } = string.Empty;

    [Phone, StringLength(Member.PhoneMaxLength)]
    public string? PhoneNumber { get; init; }

    public MembershipType MembershipType { get; init; } = MembershipType.Standard;
}

public sealed class MemberQuery : PagedQuery
{
    /// <summary>Matches first name, last name or email.</summary>
    [StringLength(100)]
    public string? Search { get; init; }

    public MembershipType? MembershipType { get; init; }

    /// <summary>When true, deactivated members are included.</summary>
    public bool IncludeInactive { get; init; }
}

internal static class MemberMappings
{
    public static MemberResponse ToResponse(this Member member) => new(
        member.Id,
        member.FirstName,
        member.LastName,
        member.FullName,
        member.Email,
        member.PhoneNumber,
        member.MembershipType,
        member.Policy.MaxActiveLoans,
        member.Policy.LoanPeriodDays,
        member.IsActive,
        member.CreatedAtUtc,
        member.UpdatedAtUtc);
}
