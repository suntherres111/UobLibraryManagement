using System.ComponentModel.DataAnnotations;
using LibraryManagement.Application.Common;
using LibraryManagement.Domain.Loans;

namespace LibraryManagement.Application.Loans;

public sealed record LoanResponse(
    int Id,
    int BookId,
    string BookTitle,
    string BookIsbn,
    int MemberId,
    string MemberName,
    DateTime BorrowedAtUtc,
    DateTime DueAtUtc,
    DateTime? ReturnedAtUtc,
    LoanStatus Status,
    int DaysOverdue,
    decimal FineAmount);

public sealed class BorrowBookRequest
{
    /// <example>1</example>
    [Range(1, int.MaxValue)]
    public int BookId { get; init; }

    /// <example>1</example>
    [Range(1, int.MaxValue)]
    public int MemberId { get; init; }
}

public sealed class LoanQuery : PagedQuery
{
    public int? MemberId { get; init; }

    public int? BookId { get; init; }

    /// <summary>Active (on loan, not yet due), Overdue, or Returned.</summary>
    public LoanStatus? Status { get; init; }
}

internal static class LoanMappings
{
    public static LoanResponse ToResponse(this Loan loan, DateTime nowUtc)
    {
        var daysOverdue = loan.IsOverdue(nowUtc)
            ? (int)Math.Ceiling((nowUtc - loan.DueAtUtc).TotalDays)
            : 0;

        return new LoanResponse(
            loan.Id,
            loan.BookId,
            loan.Book.Title,
            loan.Book.Isbn,
            loan.MemberId,
            loan.Member.FullName,
            loan.BorrowedAtUtc,
            loan.DueAtUtc,
            loan.ReturnedAtUtc,
            loan.GetStatus(nowUtc),
            daysOverdue,
            loan.FineAmount);
    }
}
