using LibraryManagement.Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Persistence.Configurations;

internal sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books", table =>
        {
            table.HasCheckConstraint("CK_Books_TotalCopies", "[TotalCopies] >= 1");
            table.HasCheckConstraint("CK_Books_AvailableCopies", "[AvailableCopies] >= 0 AND [AvailableCopies] <= [TotalCopies]");
        });

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Isbn).HasMaxLength(Book.IsbnMaxLength).IsUnicode(false).IsRequired();
        builder.HasIndex(b => b.Isbn).IsUnique();

        builder.Property(b => b.Title).HasMaxLength(Book.TitleMaxLength).IsRequired();
        builder.Property(b => b.Author).HasMaxLength(Book.AuthorMaxLength).IsRequired();
        builder.Property(b => b.Publisher).HasMaxLength(Book.PublisherMaxLength);
        builder.Property(b => b.Genre).HasMaxLength(Book.GenreMaxLength);

        // Optimistic concurrency: an UPDATE only succeeds if AvailableCopies still has the value
        // we read, so two simultaneous borrows of the last copy cannot both succeed.
        builder.Property(b => b.AvailableCopies).IsConcurrencyToken();

        builder.HasIndex(b => b.Title);
        builder.HasIndex(b => b.Author);

        builder.Ignore(b => b.CopiesOnLoan);
        builder.Ignore(b => b.IsAvailable);
    }
}
