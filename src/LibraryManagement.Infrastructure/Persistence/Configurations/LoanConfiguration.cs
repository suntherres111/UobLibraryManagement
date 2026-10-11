using LibraryManagement.Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Persistence.Configurations;

internal sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans", table =>
        {
            table.HasCheckConstraint("CK_Loans_DueAfterBorrow", "[DueAtUtc] > [BorrowedAtUtc]");
            table.HasCheckConstraint("CK_Loans_FineNotNegative", "[FineAmount] >= 0");
        });

        builder.HasKey(l => l.Id);

        // Restrict delete: loan history must never be lost by deleting a book or member.
        builder.HasOne(l => l.Book)
            .WithMany()
            .HasForeignKey(l => l.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Member)
            .WithMany()
            .HasForeignKey(l => l.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.FineAmount).HasPrecision(10, 2);

        // Database-level guarantee: a member cannot hold two open loans for the same book.
        builder.HasIndex(l => new { l.MemberId, l.BookId })
            .IsUnique()
            .HasFilter("[ReturnedAtUtc] IS NULL")
            .HasDatabaseName("UX_Loans_Member_Book_Open");

        // Supports "active loans for member" and "overdue loans" queries.
        builder.HasIndex(l => new { l.MemberId, l.ReturnedAtUtc });
        builder.HasIndex(l => new { l.ReturnedAtUtc, l.DueAtUtc });
    }
}
