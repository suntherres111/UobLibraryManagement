using LibraryManagement.Application.Common;
using LibraryManagement.Application.Loans;
using LibraryManagement.Domain.Books;
using LibraryManagement.Domain.Common;
using LibraryManagement.Domain.Loans;
using LibraryManagement.Domain.Members;
using LibraryManagement.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Tests.Application;

public sealed class LoanServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Borrow_saves_loan_and_reduces_stock_in_one_transaction()
    {
        var (bookId, memberId) = await SeedAsync(copies: 2);

        await using (var context = _database.CreateContext())
        {
            var loan = await _database.CreateLoanService(context)
                .BorrowBookAsync(new BorrowBookRequest { BookId = bookId, MemberId = memberId });

            Assert.Equal(LoanStatus.Active, loan.Status);
            Assert.Equal("Clean Code", loan.BookTitle);
        }

        await using var verify = _database.CreateContext();
        var book = await verify.Books.SingleAsync(b => b.Id == bookId);
        Assert.Equal(1, book.AvailableCopies);
        Assert.Equal(1, await verify.Loans.CountAsync(l => l.BookId == bookId && l.ReturnedAtUtc == null));
    }

    [Fact]
    public async Task Return_after_due_date_records_fine_and_restores_stock()
    {
        var (bookId, memberId) = await SeedAsync(copies: 1);
        int loanId;

        await using (var context = _database.CreateContext())
        {
            loanId = (await _database.CreateLoanService(context)
                .BorrowBookAsync(new BorrowBookRequest { BookId = bookId, MemberId = memberId })).Id;
        }

        _database.Clock.Advance(TimeSpan.FromDays(16)); // Standard: 14 days + 2 days late

        await using (var context = _database.CreateContext())
        {
            var overdue = await _database.CreateLoanService(context).GetLoansAsync(new LoanQuery { Status = LoanStatus.Overdue });
            Assert.Contains(overdue.Items, l => l.Id == loanId && l.DaysOverdue == 2);

            var returned = await _database.CreateLoanService(context).ReturnBookAsync(loanId);

            Assert.Equal(LoanStatus.Returned, returned.Status);
            Assert.Equal(2.00m, returned.FineAmount);
        }

        await using var verify = _database.CreateContext();
        Assert.Equal(1, (await verify.Books.SingleAsync(b => b.Id == bookId)).AvailableCopies);
    }

    [Fact]
    public async Task Borrow_for_unknown_member_returns_not_found()
    {
        var (bookId, _) = await SeedAsync(copies: 1);

        await using var context = _database.CreateContext();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _database.CreateLoanService(context).BorrowBookAsync(new BorrowBookRequest { BookId = bookId, MemberId = 999_999 }));
    }

    [Fact]
    public async Task Two_simultaneous_borrows_of_the_last_copy_cannot_both_succeed()
    {
        var (bookId, firstMemberId) = await SeedAsync(copies: 1);
        int secondMemberId;
        await using (var seed = _database.CreateContext())
        {
            var second = new Member("Mei Ling", "Tan", "meiling@example.com", null, MembershipType.Standard);
            seed.Members.Add(second);
            await seed.SaveChangesAsync();
            secondMemberId = second.Id;
        }

        // Request B reads the book (1 copy available) before request A commits.
        await using var contextB = _database.CreateContext();
        _ = await contextB.Books.SingleAsync(b => b.Id == bookId);

        // Request A borrows the last copy and commits.
        await using (var contextA = _database.CreateContext())
        {
            await _database.CreateLoanService(contextA)
                .BorrowBookAsync(new BorrowBookRequest { BookId = bookId, MemberId = firstMemberId });
        }

        // Request B still believes a copy is available, but the concurrency token stops it.
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _database.CreateLoanService(contextB).BorrowBookAsync(new BorrowBookRequest { BookId = bookId, MemberId = secondMemberId }));
        Assert.Equal(ErrorCodes.ConcurrencyConflict, ex.Code);

        await using var verify = _database.CreateContext();
        Assert.Equal(0, (await verify.Books.SingleAsync(b => b.Id == bookId)).AvailableCopies);
        Assert.Equal(1, await verify.Loans.CountAsync(l => l.BookId == bookId));
    }

    private async Task<(int BookId, int MemberId)> SeedAsync(int copies)
    {
        await using var context = _database.CreateContext();
        var book = new Book(TestData.UniqueIsbn(), "Clean Code", "Robert C. Martin", 2008, copies);
        var member = new Member("Ahmad", "Razak", $"ahmad{Guid.NewGuid():N}@example.com", null, MembershipType.Standard);
        context.AddRange(book, member);
        await context.SaveChangesAsync();
        return (book.Id, member.Id);
    }
}
