using LibraryManagement.Application.Books;
using LibraryManagement.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

/// <summary>Catalogue management.</summary>
[ApiController]
[Route("api/books")]
[Produces("application/json")]
public sealed class BooksController : ControllerBase
{
    private readonly IBookService _books;

    public BooksController(IBookService books)
    {
        _books = books;
    }

    /// <summary>Searches the catalogue (paged).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<BookResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BookResponse>>> GetBooks([FromQuery] BookQuery query, CancellationToken cancellationToken)
        => Ok(await _books.GetBooksAsync(query, cancellationToken));

    /// <summary>Gets one book, including current availability.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<BookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookResponse>> GetBook(int id, CancellationToken cancellationToken)
        => Ok(await _books.GetBookAsync(id, cancellationToken));

    /// <summary>Adds a new title to the catalogue.</summary>
    [HttpPost]
    [ProducesResponseType<BookResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookResponse>> CreateBook(CreateBookRequest request, CancellationToken cancellationToken)
    {
        var book = await _books.CreateBookAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
    }

    /// <summary>Updates book details and stock. ISBN cannot be changed.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<BookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookResponse>> UpdateBook(int id, UpdateBookRequest request, CancellationToken cancellationToken)
        => Ok(await _books.UpdateBookAsync(id, request, cancellationToken));

    /// <summary>Retires a book from the catalogue (soft delete; loan history is kept).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RetireBook(int id, CancellationToken cancellationToken)
    {
        await _books.RetireBookAsync(id, cancellationToken);
        return NoContent();
    }
}
