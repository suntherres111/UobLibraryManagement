using System.Net.Mail;
using LibraryManagement.Domain.Common;

namespace LibraryManagement.Domain.Members;

/// <summary>A registered library member who can borrow books.</summary>
public sealed class Member : Entity
{
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 256;
    public const int PhoneMaxLength = 20;

    // Required by EF Core for materialisation.
    private Member()
    {
    }

    public Member(string firstName, string lastName, string email, string? phoneNumber, MembershipType membershipType)
    {
        UpdateDetails(firstName, lastName, email, phoneNumber);
        ChangeMembershipType(membershipType);
        IsActive = true;
    }

    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public string? PhoneNumber { get; private set; }

    public MembershipType MembershipType { get; private set; }

    public bool IsActive { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    /// <summary>Borrowing rules for this member's tier (not persisted; derived from MembershipType).</summary>
    public BorrowingPolicy Policy => BorrowingPolicy.For(MembershipType);

    public static string NormalizeEmail(string? email)
    {
        var value = Guard.Required(email, nameof(Email), EmailMaxLength).ToLowerInvariant();

        if (!MailAddress.TryCreate(value, out var parsed) || parsed.Address != value)
        {
            throw new DomainValidationException("Email address is not valid.");
        }

        return value;
    }

    public void UpdateDetails(string firstName, string lastName, string email, string? phoneNumber)
    {
        FirstName = Guard.Required(firstName, nameof(FirstName), NameMaxLength);
        LastName = Guard.Required(lastName, nameof(LastName), NameMaxLength);
        Email = NormalizeEmail(email);
        PhoneNumber = Guard.Optional(phoneNumber, nameof(PhoneNumber), PhoneMaxLength);
    }

    public void ChangeMembershipType(MembershipType membershipType)
    {
        if (!Enum.IsDefined(membershipType))
        {
            throw new DomainValidationException("Membership type is not valid.");
        }

        MembershipType = membershipType;
    }

    /// <summary>Deactivates the membership (soft delete). Members must return all books first.</summary>
    public void Deactivate(int activeLoanCount)
    {
        if (activeLoanCount > 0)
        {
            throw new DomainException(
                ErrorCodes.MemberHasActiveLoans,
                $"Member still has {activeLoanCount} book(s) on loan and cannot be deactivated.");
        }

        IsActive = false;
    }

    public void Reactivate() => IsActive = true;
}
