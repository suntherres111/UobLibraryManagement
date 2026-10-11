using LibraryManagement.Domain.Books;
using LibraryManagement.Domain.Loans;
using LibraryManagement.Domain.Members;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Application.Abstractions;

/// <summary>
/// Persistence abstraction used by application services.
/// EF Core's DbContext already implements the Unit of Work and Repository patterns,
/// so services depend on this interface instead of a hand-written repository layer.
/// The Application layer never references SQL Server directly.
/// </summary>
public interface ILibraryDbContext
{
    DbSet<Book> Books { get; }

    DbSet<Member> Members { get; }

    DbSet<Loan> Loans { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
