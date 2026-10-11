using LibraryManagement.Domain.Common;
using LibraryManagement.Domain.Lending;
using LibraryManagement.Domain.Loans;
using LibraryManagement.Domain.Members;
using LibraryManagement.Tests.Support;

namespace LibraryManagement.Tests.Domain;

public sealed class LibraryTests
{
    private readonly TestClock _clock = new();
    private readonly Library _library;

    public LibraryTests()
    {
        _library = new Library(_clock);
    }

    [Fact]
    public void CheckOut_takes_a_copy_and_sets_due_date_from_policy()
    {
        var book = TestData.Book(copies: 2);
        var member = TestData.Member(MembershipType.Standard);

        var loan = _library.CheckOut(book, member, []);

        Assert.Equal(1, book.AvailableCopies);
        Assert.Equal(_clock.UtcNow, loan.BorrowedAtUtc);
        Assert.Equal(_clock.UtcNow.AddDays(14), loan.DueAtUtc);
        Assert.Equal(LoanStatus.Active, loan.GetStatus(_clock.UtcNow));
    }

    [Fact]
    public void Premium_members_get_a_longer_loan_period()
    {
        var loan = _library.CheckOut(TestData.Book(), TestData.Member(MembershipType.Premium), []);

        Assert.Equal(_clock.UtcNow.AddDays(30), loan.DueAtUtc);
    }

    [Fact]
    public void CheckOut_fails_when_no_copies_available()
    {
        var book = TestData.Book(copies: 1);
        _library.CheckOut(book, TestData.Member(), []);

        AssertRule(ErrorCodes.BookUnavailable, () => _library.CheckOut(book, TestData.Member(), []));
    }

    [Fact]
    public void CheckOut_fails_for_inactive_member()
    {
        var member = TestData.Member();
        member.Deactivate(activeLoanCount: 0);

        AssertRule(ErrorCodes.MemberInactive, () => _library.CheckOut(TestData.Book(), member, []));
    }

    [Fact]
    public void CheckOut_fails_for_retired_book()
    {
        var book = TestData.Book();
        book.Retire();

        AssertRule(ErrorCodes.BookInactive, () => _library.CheckOut(book, TestData.Member(), []));
    }

    [Fact]
    public void CheckOut_enforces_loan_limit_of_membership_tier()
    {
        var member = TestData.Member(MembershipType.Standard);
        var activeLoans = new List<Loan>();
        for (var i = 0; i < member.Policy.MaxActiveLoans; i++)
        {
            activeLoans.Add(_library.CheckOut(TestData.Book(), member, activeLoans));
        }

        var nextBook = TestData.Book();
        AssertRule(ErrorCodes.LoanLimitReached, () => _library.CheckOut(nextBook, member, activeLoans));
        Assert.Equal(1, nextBook.AvailableCopies); // nothing changed when the rule failed
    }

    [Fact]
    public void CheckOut_is_blocked_while_member_has_overdue_books()
    {
        var member = TestData.Member();
        var loan = _library.CheckOut(TestData.Book(), member, []);

        _clock.Advance(TimeSpan.FromDays(15));

        AssertRule(ErrorCodes.MemberHasOverdueLoans, () => _library.CheckOut(TestData.Book(), member, [loan]));
    }

    [Fact]
    public void CheckOut_prevents_borrowing_two_copies_of_same_book()
    {
        var member = TestData.Member();
        var book = TestData.Book(copies: 3);
        var loan = _library.CheckOut(book, member, []);

        AssertRule(ErrorCodes.BookAlreadyBorrowed, () => _library.CheckOut(book, member, [loan]));
    }

    [Fact]
    public void CheckIn_on_time_returns_copy_without_fine()
    {
        var book = TestData.Book();
        var loan = _library.CheckOut(book, TestData.Member(), []);
        _clock.Advance(TimeSpan.FromDays(7));

        _library.CheckIn(loan);

        Assert.Equal(LoanStatus.Returned, loan.GetStatus(_clock.UtcNow));
        Assert.Equal(_clock.UtcNow, loan.ReturnedAtUtc);
        Assert.Equal(0m, loan.FineAmount);
        Assert.Equal(1, book.AvailableCopies);
    }

    [Fact]
    public void CheckIn_late_records_fine()
    {
        var loan = _library.CheckOut(TestData.Book(), TestData.Member(MembershipType.Standard), []);
        _clock.Advance(TimeSpan.FromDays(14 + 3));

        _library.CheckIn(loan);

        Assert.Equal(3.00m, loan.FineAmount);
    }

    [Fact]
    public void CheckIn_twice_is_rejected()
    {
        var book = TestData.Book(copies: 2);
        var loan = _library.CheckOut(book, TestData.Member(), []);
        _library.CheckIn(loan);

        AssertRule(ErrorCodes.LoanAlreadyReturned, () => _library.CheckIn(loan));
        Assert.Equal(2, book.AvailableCopies);
    }

    private static void AssertRule(string expectedCode, Action action)
    {
        var ex = Assert.Throws<DomainException>(action);
        Assert.Equal(expectedCode, ex.Code);
    }
}
