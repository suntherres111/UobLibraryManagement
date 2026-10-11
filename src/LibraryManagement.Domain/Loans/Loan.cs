using LibraryManagement.Domain.Books;
using LibraryManagement.Domain.Common;
using LibraryManagement.Domain.Members;

namespace LibraryManagement.Domain.Loans;

/// <summary>
/// Records one copy of a book borrowed by a member.
/// Loans are created and closed only by the <see cref="Lending.Library"/> domain service.
/// "Overdue" is calculated from the current time rather than stored, so it can never go stale.
/// </summary>
public sealed class Loan : Entity
{
    // Required by EF Core for materialisation.
    private Loan()
    {
    }

    internal Loan(Book book, Member member, DateTime borrowedAtUtc, DateTime dueAtUtc)
    {
        Book = book;
        BookId = book.Id;
        Member = member;
        MemberId = member.Id;
        BorrowedAtUtc = borrowedAtUtc;
        DueAtUtc = dueAtUtc;
    }

    public int BookId { get; private set; }

    public Book Book { get; private set; } = null!;

    public int MemberId { get; private set; }

    public Member Member { get; private set; } = null!;

    public DateTime BorrowedAtUtc { get; private set; }

    public DateTime DueAtUtc { get; private set; }

    public DateTime? ReturnedAtUtc { get; private set; }

    /// <summary>Late fine in MYR, fixed at the moment the book is returned.</summary>
    public decimal FineAmount { get; private set; }

    public bool IsReturned => ReturnedAtUtc.HasValue;

    public bool IsOverdue(DateTime nowUtc) => !IsReturned && nowUtc > DueAtUtc;

    public LoanStatus GetStatus(DateTime nowUtc)
    {
        if (IsReturned)
        {
            return LoanStatus.Returned;
        }

        return IsOverdue(nowUtc) ? LoanStatus.Overdue : LoanStatus.Active;
    }

    internal void MarkReturned(DateTime returnedAtUtc, decimal fineAmount)
    {
        if (IsReturned)
        {
            throw new DomainException(ErrorCodes.LoanAlreadyReturned, "This loan has already been returned.");
        }

        ReturnedAtUtc = returnedAtUtc;
        FineAmount = fineAmount;
    }
}
