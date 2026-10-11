using LibraryManagement.Domain.Books;
using LibraryManagement.Domain.Common;
using LibraryManagement.Domain.Loans;
using LibraryManagement.Domain.Members;

namespace LibraryManagement.Domain.Lending;

/// <summary>
/// The Library domain service: owns the lending rules that span Book, Member and Loan.
/// It is the only code allowed to change book stock or open/close loans
/// (those members are <c>internal</c> to the Domain assembly).
/// It has no database dependency, so every rule is unit-testable in isolation.
/// </summary>
public sealed class Library
{
    private readonly TimeProvider _clock;

    public Library(TimeProvider clock)
    {
        _clock = clock;
    }

    /// <summary>Lends one copy of <paramref name="book"/> to <paramref name="member"/>.</summary>
    /// <param name="memberActiveLoans">All loans the member has not yet returned.</param>
    public Loan CheckOut(Book book, Member member, IReadOnlyCollection<Loan> memberActiveLoans)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(memberActiveLoans);

        var now = UtcNow();

        if (!member.IsActive)
        {
            throw new DomainException(ErrorCodes.MemberInactive, "Inactive members cannot borrow books.");
        }

        if (memberActiveLoans.Any(l => l.IsOverdue(now)))
        {
            throw new DomainException(
                ErrorCodes.MemberHasOverdueLoans,
                "Member has overdue books and must return them before borrowing again.");
        }

        var policy = member.Policy;
        if (memberActiveLoans.Count >= policy.MaxActiveLoans)
        {
            throw new DomainException(
                ErrorCodes.LoanLimitReached,
                $"{policy.MembershipType} members may borrow at most {policy.MaxActiveLoans} books at a time.");
        }

        if (memberActiveLoans.Any(l => l.BookId == book.Id))
        {
            throw new DomainException(
                ErrorCodes.BookAlreadyBorrowed,
                "Member already has a copy of this book on loan.");
        }

        book.CheckOutCopy();

        return new Loan(book, member, now, policy.CalculateDueDate(now));
    }

    /// <summary>Closes the loan, calculates any late fine and puts the copy back on the shelf.</summary>
    public void CheckIn(Loan loan)
    {
        ArgumentNullException.ThrowIfNull(loan);

        var now = UtcNow();
        var fine = loan.Member.Policy.CalculateFine(loan.DueAtUtc, now);

        loan.MarkReturned(now, fine);
        loan.Book.ReturnCopy();
    }

    private DateTime UtcNow() => _clock.GetUtcNow().UtcDateTime;
}
