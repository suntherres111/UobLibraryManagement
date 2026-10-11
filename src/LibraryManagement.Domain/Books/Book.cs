using System.Text.RegularExpressions;
using LibraryManagement.Domain.Common;

namespace LibraryManagement.Domain.Books;

/// <summary>
/// A catalogue title held by the library. Tracks how many physical copies exist and
/// how many are currently on the shelf. Stock can only change through the methods below,
/// so the invariant 0 &lt;= AvailableCopies &lt;= TotalCopies always holds.
/// </summary>
public sealed class Book : Entity
{
    public const int IsbnMaxLength = 13;
    public const int TitleMaxLength = 200;
    public const int AuthorMaxLength = 150;
    public const int PublisherMaxLength = 150;
    public const int GenreMaxLength = 50;
    public const int MinPublishedYear = 1450;

    private static readonly Regex IsbnPattern = new(@"^(\d{9}[\dX]|\d{13})$", RegexOptions.Compiled);

    // Required by EF Core for materialisation.
    private Book()
    {
    }

    public Book(
        string isbn,
        string title,
        string author,
        int publishedYear,
        int totalCopies,
        string? publisher = null,
        string? genre = null)
    {
        Isbn = NormalizeIsbn(isbn);
        SetDetails(title, author, publishedYear, publisher, genre);

        if (totalCopies < 1)
        {
            throw new DomainValidationException("A book must have at least one copy.");
        }

        TotalCopies = totalCopies;
        AvailableCopies = totalCopies;
        IsActive = true;
    }

    public string Isbn { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public string Author { get; private set; } = null!;

    public string? Publisher { get; private set; }

    public string? Genre { get; private set; }

    public int PublishedYear { get; private set; }

    public int TotalCopies { get; private set; }

    public int AvailableCopies { get; private set; }

    /// <summary>False when the title has been retired from the catalogue (soft delete).</summary>
    public bool IsActive { get; private set; }

    public int CopiesOnLoan => TotalCopies - AvailableCopies;

    public bool IsAvailable => IsActive && AvailableCopies > 0;

    /// <summary>Removes hyphens/spaces and validates ISBN-10 or ISBN-13 format.</summary>
    public static string NormalizeIsbn(string? isbn)
    {
        var normalized = (isbn ?? string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty)
            .Trim()
            .ToUpperInvariant();

        if (!IsbnPattern.IsMatch(normalized))
        {
            throw new DomainValidationException("ISBN must be a valid ISBN-10 or ISBN-13.");
        }

        return normalized;
    }

    public void UpdateDetails(string title, string author, int publishedYear, string? publisher, string? genre)
        => SetDetails(title, author, publishedYear, publisher, genre);

    /// <summary>
    /// Adjusts stock (e.g. new copies purchased, or damaged copies written off).
    /// Copies that are currently on loan cannot be removed.
    /// </summary>
    public void ChangeTotalCopies(int newTotalCopies)
    {
        if (newTotalCopies < 1)
        {
            throw new DomainValidationException("A book must have at least one copy.");
        }

        if (newTotalCopies < CopiesOnLoan)
        {
            throw new DomainException(
                ErrorCodes.CopiesBelowOnLoan,
                $"Cannot reduce total copies to {newTotalCopies}; {CopiesOnLoan} copies are currently on loan.");
        }

        var onLoan = CopiesOnLoan;
        TotalCopies = newTotalCopies;
        AvailableCopies = newTotalCopies - onLoan;
    }

    /// <summary>Retires the title from the catalogue. History (loans) is preserved.</summary>
    public void Retire()
    {
        if (CopiesOnLoan > 0)
        {
            throw new DomainException(
                ErrorCodes.BookHasActiveLoans,
                "Cannot remove a book while copies are still on loan.");
        }

        IsActive = false;
    }

    /// <summary>Only the Library lending service may take a copy off the shelf.</summary>
    internal void CheckOutCopy()
    {
        if (!IsActive)
        {
            throw new DomainException(ErrorCodes.BookInactive, $"Book '{Title}' is no longer in the catalogue.");
        }

        if (AvailableCopies == 0)
        {
            throw new DomainException(ErrorCodes.BookUnavailable, $"No copies of '{Title}' are currently available.");
        }

        AvailableCopies--;
    }

    /// <summary>Only the Library lending service may put a copy back on the shelf.</summary>
    internal void ReturnCopy()
    {
        if (AvailableCopies >= TotalCopies)
        {
            throw new InvalidOperationException("All copies are already on the shelf; stock is inconsistent.");
        }

        AvailableCopies++;
    }

    private void SetDetails(string title, string author, int publishedYear, string? publisher, string? genre)
    {
        var maxYear = DateTime.UtcNow.Year + 1;
        if (publishedYear < MinPublishedYear || publishedYear > maxYear)
        {
            throw new DomainValidationException($"Published year must be between {MinPublishedYear} and {maxYear}.");
        }

        Title = Guard.Required(title, nameof(Title), TitleMaxLength);
        Author = Guard.Required(author, nameof(Author), AuthorMaxLength);
        Publisher = Guard.Optional(publisher, nameof(Publisher), PublisherMaxLength);
        Genre = Guard.Optional(genre, nameof(Genre), GenreMaxLength);
        PublishedYear = publishedYear;
    }
}
