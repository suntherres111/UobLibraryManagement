using LibraryManagement.Domain.Members;

namespace LibraryManagement.Tests.Domain;

public sealed class BorrowingPolicyTests
{
    private static readonly DateTime Due = new(2026, 10, 15, 9, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(-60, 0.0)]   // returned an hour early
    [InlineData(0, 0.0)]     // returned exactly on time
    [InlineData(1, 1.00)]    // one minute late counts as one day
    [InlineData(3 * 1440, 3.00)]
    [InlineData(100 * 1440, 50.00)] // capped at RM50
    public void Standard_fine_is_RM1_per_started_day(int minutesLate, double expectedFine)
    {
        var fine = BorrowingPolicy.For(MembershipType.Standard).CalculateFine(Due, Due.AddMinutes(minutesLate));

        Assert.Equal((decimal)expectedFine, fine);
    }

    [Theory]
    [InlineData(1, 0.0)]        // within the 1-day grace period
    [InlineData(3, 1.00)]       // 2 chargeable days x RM0.50
    public void Premium_fine_has_grace_period_and_lower_rate(int daysLate, double expectedFine)
    {
        var fine = BorrowingPolicy.For(MembershipType.Premium).CalculateFine(Due, Due.AddDays(daysLate));

        Assert.Equal((decimal)expectedFine, fine);
    }

    [Fact]
    public void Each_tier_has_its_own_limits()
    {
        var standard = BorrowingPolicy.For(MembershipType.Standard);
        var premium = BorrowingPolicy.For(MembershipType.Premium);

        Assert.IsType<StandardBorrowingPolicy>(standard);
        Assert.IsType<PremiumBorrowingPolicy>(premium);
        Assert.True(premium.MaxActiveLoans > standard.MaxActiveLoans);
        Assert.True(premium.LoanPeriodDays > standard.LoanPeriodDays);
    }
}
