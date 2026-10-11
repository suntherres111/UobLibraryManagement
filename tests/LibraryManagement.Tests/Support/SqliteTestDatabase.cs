using LibraryManagement.Application.Loans;
using LibraryManagement.Domain.Lending;
using LibraryManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Tests.Support;

/// <summary>
/// Real relational database (SQLite in-memory) so tests exercise EF mappings, unique indexes,
/// check constraints and concurrency tokens — which the EF InMemory provider would ignore.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public TestClock Clock { get; } = new();

    public LibraryDbContext CreateContext()
        => new(new DbContextOptionsBuilder<LibraryDbContext>().UseSqlite(_connection).Options, Clock);

    public LoanService CreateLoanService(LibraryDbContext context) => new(context, new Library(Clock), Clock);

    public void Dispose() => _connection.Dispose();
}
