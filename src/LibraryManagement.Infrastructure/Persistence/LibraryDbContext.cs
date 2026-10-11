using LibraryManagement.Application.Abstractions;
using LibraryManagement.Domain.Books;
using LibraryManagement.Domain.Common;
using LibraryManagement.Domain.Loans;
using LibraryManagement.Domain.Members;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Persistence;

public sealed class LibraryDbContext : DbContext, ILibraryDbContext
{
    private readonly TimeProvider _clock;

    public LibraryDbContext(DbContextOptions<LibraryDbContext> options, TimeProvider clock)
        : base(options)
    {
        _clock = clock;
    }

    public DbSet<Book> Books => Set<Book>();

    public DbSet<Member> Members => Set<Member>();

    public DbSet<Loan> Loans => Set<Loan>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
    }

    /// <summary>Sets CreatedAtUtc / UpdatedAtUtc centrally so no service can forget to.</summary>
    private void ApplyAuditTimestamps()
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.MarkCreated(now);
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.MarkUpdated(now);
            }
        }
    }
}
