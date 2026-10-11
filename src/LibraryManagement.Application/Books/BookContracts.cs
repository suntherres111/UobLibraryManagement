using System.ComponentModel.DataAnnotations;
using LibraryManagement.Application.Common;
using LibraryManagement.Domain.Books;

namespace LibraryManagement.Application.Books;

public sealed record BookResponse(
    int Id,
    string Isbn,
    string Title,
    string Author,
    string? Publisher,
    string? Genre,
    int PublishedYear,
    int TotalCopies,
    int AvailableCopies,
    bool IsAvailable,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed class CreateBookRequest
{
    /// <example>978-0132350884</example>
    [Required, StringLength(17, MinimumLength = 10)]
    public string Isbn { get; init; } = string.Empty;

    /// <example>Clean Code</example>
    [Required, StringLength(Book.TitleMaxLength)]
    public string Title { get; init; } = string.Empty;

    /// <example>Robert C. Martin</example>
    [Required, StringLength(Book.AuthorMaxLength)]
    public string Author { get; init; } = string.Empty;

    [StringLength(Book.PublisherMaxLength)]
    public string? Publisher { get; init; }

    [StringLength(Book.GenreMaxLength)]
    public string? Genre { get; init; }

    /// <example>2008</example>
    [Range(Book.MinPublishedYear, 2100)]
    public int PublishedYear { get; init; }

    /// <example>3</example>
    [Range(1, 1000)]
    public int TotalCopies { get; init; } = 1;
}

/// <summary>ISBN is the book's identity and cannot be changed after creation.</summary>
public sealed class UpdateBookRequest
{
    [Required, StringLength(Book.TitleMaxLength)]
    public string Title { get; init; } = string.Empty;

    [Required, StringLength(Book.AuthorMaxLength)]
    public string Author { get; init; } = string.Empty;

    [StringLength(Book.PublisherMaxLength)]
    public string? Publisher { get; init; }

    [StringLength(Book.GenreMaxLength)]
    public string? Genre { get; init; }

    [Range(Book.MinPublishedYear, 2100)]
    public int PublishedYear { get; init; }

    [Range(1, 1000)]
    public int TotalCopies { get; init; } = 1;
}

public sealed class BookQuery : PagedQuery
{
    /// <summary>Matches title, author or ISBN.</summary>
    [StringLength(100)]
    public string? Search { get; init; }

    [StringLength(Book.GenreMaxLength)]
    public string? Genre { get; init; }

    /// <summary>When true, only books with at least one copy on the shelf.</summary>
    public bool? AvailableOnly { get; init; }

    /// <summary>When true, retired books are included.</summary>
    public bool IncludeInactive { get; init; }
}

internal static class BookMappings
{
    public static BookResponse ToResponse(this Book book) => new(
        book.Id,
        book.Isbn,
        book.Title,
        book.Author,
        book.Publisher,
        book.Genre,
        book.PublishedYear,
        book.TotalCopies,
        book.AvailableCopies,
        book.IsAvailable,
        book.IsActive,
        book.CreatedAtUtc,
        book.UpdatedAtUtc);
}
