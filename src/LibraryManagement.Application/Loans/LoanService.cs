using LibraryManagement.Application.Abstractions;
using LibraryManagement.Application.Common;
using LibraryManagement.Domain.Books;
using LibraryManagement.Domain.Lending;
using LibraryManagement.Domain.Loans;
using LibraryManagement.Domain.Members;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Application.Loans;

public interface ILoanService
{
    Task<PagedResult<LoanResponse>> GetLoansAsync(LoanQuery query, CancellationToken cancellationToken = default);

    Task<LoanResponse> GetLoanAsync(int id, CancellationToken cancellationToken = default);

    Task<LoanResponse> BorrowBookAsync(BorrowBookRequest request, CancellationToken cancellationToken = default);

    Task<LoanResponse> ReturnBookAsync(int loanId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Application service for lending. It loads data, delegates every business decision to the
/// <see cref="Library"/> domain service, then persists the result in a single SaveChanges
/// (one transaction: the new loan and the stock change succeed or fail together).
/// </summary>
public sealed class LoanService : ILoanService
{
    private readonly ILibraryDbContext _db;
    private readonly Library _library;
    private readonly TimeProvider _clock;

    public LoanService(ILibraryDbContext db, Library library, TimeProvider clock)
    {
        _db = db;
        _library = library;
        _clock = clock;
    }

    public async Task<PagedResult<LoanResponse>> GetLoansAsync(LoanQuery query, CancellationToken cancellationToken = default)
    {
        var now = UtcNow();
        var loans = _db.Loans
            .AsNoTracking()
            .Include(l => l.Book)
            .Include(l => l.Member)
            .AsQueryable();

        if (query.MemberId.HasValue)
        {
            loans = loans.Where(l => l.MemberId == query.MemberId.Value);
        }

        if (query.BookId.HasValue)
        {
            loans = loans.Where(l => l.BookId == query.BookId.Value);
        }

        loans = query.Status switch
        {
            LoanStatus.Returned => loans.Where(l => l.ReturnedAtUtc != null),
            LoanStatus.Overdue => loans.Where(l => l.ReturnedAtUtc == null && l.DueAtUtc < now),
            LoanStatus.Active => loans.Where(l => l.ReturnedAtUtc == null && l.DueAtUtc >= now),
            _ => loans
        };

        var (items, total) = await loans
            .OrderByDescending(l => l.BorrowedAtUtc)
            .ThenByDescending(l => l.Id)
            .ToPageAsync(query, cancellationToken);

        return new PagedResult<LoanResponse>(items.Select(l => l.ToResponse(now)).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<LoanResponse> GetLoanAsync(int id, CancellationToken cancellationToken = default)
    {
        var loan = await _db.Loans
            .AsNoTracking()
            .Include(l => l.Book)
            .Include(l => l.Member)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Loan), id);

        return loan.ToResponse(UtcNow());
    }

    public async Task<LoanResponse> BorrowBookAsync(BorrowBookRequest request, CancellationToken cancellationToken = default)
    {
        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == request.MemberId, cancellationToken)
            ?? throw new NotFoundException(nameof(Member), request.MemberId);

        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == request.BookId, cancellationToken)
            ?? throw new NotFoundException(nameof(Book), request.BookId);

        var activeLoans = await _db.Loans
            .Where(l => l.MemberId == member.Id && l.ReturnedAtUtc == null)
            .ToListAsync(cancellationToken);

        var loan = _library.CheckOut(book, member, activeLoans);

        _db.Loans.Add(loan);
        await _db.SaveChangesWithConcurrencyCheckAsync(cancellationToken);

        return loan.ToResponse(UtcNow());
    }

    public async Task<LoanResponse> ReturnBookAsync(int loanId, CancellationToken cancellationToken = default)
    {
        var loan = await _db.Loans
            .Include(l => l.Book)
            .Include(l => l.Member)
            .FirstOrDefaultAsync(l => l.Id == loanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Loan), loanId);

        _library.CheckIn(loan);

        await _db.SaveChangesWithConcurrencyCheckAsync(cancellationToken);

        return loan.ToResponse(UtcNow());
    }

    private DateTime UtcNow() => _clock.GetUtcNow().UtcDateTime;
}
