using LibraryManagement.Application.Common;
using LibraryManagement.Application.Loans;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

/// <summary>Borrowing and returning books.</summary>
[ApiController]
[Route("api/loans")]
[Produces("application/json")]
public sealed class LoansController : ControllerBase
{
    private readonly ILoanService _loans;

    public LoansController(ILoanService loans)
    {
        _loans = loans;
    }

    /// <summary>Lists loans, filterable by member, book and status (Active / Overdue / Returned).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<LoanResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LoanResponse>>> GetLoans([FromQuery] LoanQuery query, CancellationToken cancellationToken)
        => Ok(await _loans.GetLoansAsync(query, cancellationToken));

    /// <summary>Gets one loan.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<LoanResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoanResponse>> GetLoan(int id, CancellationToken cancellationToken)
        => Ok(await _loans.GetLoanAsync(id, cancellationToken));

    /// <summary>Borrows a book for a member.</summary>
    /// <response code="201">Loan created; due date is set by the member's tier.</response>
    /// <response code="409">A rule blocked the loan, e.g. no copies available, loan limit reached, overdue books.</response>
    [HttpPost]
    [ProducesResponseType<LoanResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanResponse>> BorrowBook(BorrowBookRequest request, CancellationToken cancellationToken)
    {
        var loan = await _loans.BorrowBookAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetLoan), new { id = loan.Id }, loan);
    }

    /// <summary>Returns a borrowed book. Any late fine is calculated and recorded on the loan.</summary>
    [HttpPost("{id:int}/return")]
    [ProducesResponseType<LoanResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanResponse>> ReturnBook(int id, CancellationToken cancellationToken)
        => Ok(await _loans.ReturnBookAsync(id, cancellationToken));
}
