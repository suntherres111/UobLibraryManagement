namespace LibraryManagement.Domain.Members;

/// <summary>
/// Strategy describing the borrowing rules for a membership tier.
/// New tiers (e.g. Student, Staff) are added by creating a new subclass,
/// without changing the lending logic (Open/Closed Principle).
/// </summary>
public abstract class BorrowingPolicy
{
    public abstract MembershipType MembershipType { get; }

    /// <summary>Maximum number of books a member may hold at the same time.</summary>
    public abstract int MaxActiveLoans { get; }

    public abstract int LoanPeriodDays { get; }

    /// <summary>Fine charged per started day late, in MYR.</summary>
    public abstract decimal DailyFineRate { get; }

    /// <summary>Upper limit on the fine for a single loan, in MYR.</summary>
    public virtual decimal MaxFinePerLoan => 50.00m;

    public static BorrowingPolicy For(MembershipType membershipType) => membershipType switch
    {
        MembershipType.Standard => StandardBorrowingPolicy.Instance,
        MembershipType.Premium => PremiumBorrowingPolicy.Instance,
        _ => throw new ArgumentOutOfRangeException(nameof(membershipType), membershipType, "Unknown membership type.")
    };

    public DateTime CalculateDueDate(DateTime borrowedAtUtc) => borrowedAtUtc.AddDays(LoanPeriodDays);

    /// <summary>Calculates the late-return fine. Any part of a day late counts as a full day.</summary>
    public virtual decimal CalculateFine(DateTime dueAtUtc, DateTime returnedAtUtc)
    {
        var daysLate = (int)Math.Ceiling((returnedAtUtc - dueAtUtc).TotalDays);
        if (daysLate <= 0)
        {
            return 0m;
        }

        return Math.Min(daysLate * DailyFineRate, MaxFinePerLoan);
    }
}

public sealed class StandardBorrowingPolicy : BorrowingPolicy
{
    public static readonly StandardBorrowingPolicy Instance = new();

    private StandardBorrowingPolicy()
    {
    }

    public override MembershipType MembershipType => MembershipType.Standard;

    public override int MaxActiveLoans => 3;

    public override int LoanPeriodDays => 14;

    public override decimal DailyFineRate => 1.00m;
}

public sealed class PremiumBorrowingPolicy : BorrowingPolicy
{
    public static readonly PremiumBorrowingPolicy Instance = new();

    /// <summary>Premium members get one grace day before fines start.</summary>
    public const int GracePeriodDays = 1;

    private PremiumBorrowingPolicy()
    {
    }

    public override MembershipType MembershipType => MembershipType.Premium;

    public override int MaxActiveLoans => 10;

    public override int LoanPeriodDays => 30;

    public override decimal DailyFineRate => 0.50m;

    public override decimal CalculateFine(DateTime dueAtUtc, DateTime returnedAtUtc)
        => base.CalculateFine(dueAtUtc.AddDays(GracePeriodDays), returnedAtUtc);
}
