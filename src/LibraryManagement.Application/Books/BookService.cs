using LibraryManagement.Application.Abstractions;
using LibraryManagement.Application.Common;
using LibraryManagement.Domain.Books;
using LibraryManagement.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Application.Books;

public interface IBookService
{
    Task<PagedResult<BookResponse>> GetBooksAsync(BookQuery query, CancellationToken cancellationToken = default);

    Task<BookResponse> GetBookAsync(int id, CancellationToken cancellationToken = default);

    Task<BookResponse> CreateBookAsync(CreateBookRequest request, CancellationToken cancellationToken = default);

    Task<BookResponse> UpdateBookAsync(int id, UpdateBookRequest request, CancellationToken cancellationToken = default);

    Task RetireBookAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class BookService : IBookService
{
    private readonly ILibraryDbContext _db;

    public BookService(ILibraryDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<BookResponse>> GetBooksAsync(BookQuery query, CancellationToken cancellationToken = default)
    {
        var books = _db.Books.AsNoTracking();

        if (!query.IncludeInactive)
        {
            books = books.Where(b => b.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            var isbnTerm = term.Replace("-", string.Empty);
            books = books.Where(b => b.Title.Contains(term) || b.Author.Contains(term) || b.Isbn.Contains(isbnTerm));
        }

        if (!string.IsNullOrWhiteSpace(query.Genre))
        {
            var genre = query.Genre.Trim();
            books = books.Where(b => b.Genre == genre);
        }

        if (query.AvailableOnly == true)
        {
            books = books.Where(b => b.AvailableCopies > 0);
        }

        var (items, total) = await books
            .OrderBy(b => b.Title)
            .ThenBy(b => b.Id)
            .ToPageAsync(query, cancellationToken);

        return new PagedResult<BookResponse>(items.Select(b => b.ToResponse()).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<BookResponse> GetBookAsync(int id, CancellationToken cancellationToken = default)
    {
        var book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Book), id);

        return book.ToResponse();
    }

    public async Task<BookResponse> CreateBookAsync(CreateBookRequest request, CancellationToken cancellationToken = default)
    {
        var book = new Book(
            request.Isbn,
            request.Title,
            request.Author,
            request.PublishedYear,
            request.TotalCopies,
            request.Publisher,
            request.Genre);

        if (await _db.Books.AnyAsync(b => b.Isbn == book.Isbn, cancellationToken))
        {
            throw new DomainException(ErrorCodes.DuplicateIsbn, $"A book with ISBN {book.Isbn} already exists.");
        }

        _db.Books.Add(book);
        await _db.SaveChangesAsync(cancellationToken);

        return book.ToResponse();
    }

    public async Task<BookResponse> UpdateBookAsync(int id, UpdateBookRequest request, CancellationToken cancellationToken = default)
    {
        var book = await FindAsync(id, cancellationToken);

        book.UpdateDetails(request.Title, request.Author, request.PublishedYear, request.Publisher, request.Genre);
        book.ChangeTotalCopies(request.TotalCopies);

        await _db.SaveChangesWithConcurrencyCheckAsync(cancellationToken);

        return book.ToResponse();
    }

    public async Task RetireBookAsync(int id, CancellationToken cancellationToken = default)
    {
        var book = await FindAsync(id, cancellationToken);

        book.Retire();

        await _db.SaveChangesWithConcurrencyCheckAsync(cancellationToken);
    }

    private async Task<Book> FindAsync(int id, CancellationToken cancellationToken)
        => await _db.Books.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
           ?? throw new NotFoundException(nameof(Book), id);
}
