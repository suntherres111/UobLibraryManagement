using LibraryManagement.Application.Common;
using LibraryManagement.Application.Loans;
using LibraryManagement.Application.Members;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

/// <summary>Member registration and management.</summary>
[ApiController]
[Route("api/members")]
[Produces("application/json")]
public sealed class MembersController : ControllerBase
{
    private readonly IMemberService _members;
    private readonly ILoanService _loans;

    public MembersController(IMemberService members, ILoanService loans)
    {
        _members = members;
        _loans = loans;
    }

    /// <summary>Lists members (paged).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<MemberResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MemberResponse>>> GetMembers([FromQuery] MemberQuery query, CancellationToken cancellationToken)
        => Ok(await _members.GetMembersAsync(query, cancellationToken));

    /// <summary>Gets one member, including their borrowing limits.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<MemberResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberResponse>> GetMember(int id, CancellationToken cancellationToken)
        => Ok(await _members.GetMemberAsync(id, cancellationToken));

    /// <summary>Registers a new member.</summary>
    [HttpPost]
    [ProducesResponseType<MemberResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MemberResponse>> CreateMember(CreateMemberRequest request, CancellationToken cancellationToken)
    {
        var member = await _members.CreateMemberAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetMember), new { id = member.Id }, member);
    }

    /// <summary>Updates member details or membership tier.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<MemberResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MemberResponse>> UpdateMember(int id, UpdateMemberRequest request, CancellationToken cancellationToken)
        => Ok(await _members.UpdateMemberAsync(id, request, cancellationToken));

    /// <summary>Deactivates a member (soft delete). Fails if the member still has books on loan.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateMember(int id, CancellationToken cancellationToken)
    {
        await _members.DeactivateMemberAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Reactivates a previously deactivated member.</summary>
    [HttpPost("{id:int}/reactivate")]
    [ProducesResponseType<MemberResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberResponse>> ReactivateMember(int id, CancellationToken cancellationToken)
        => Ok(await _members.ReactivateMemberAsync(id, cancellationToken));

    /// <summary>Gets a member's borrowing history (paged, newest first).</summary>
    [HttpGet("{id:int}/loans")]
    [ProducesResponseType<PagedResult<LoanResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<LoanResponse>>> GetMemberLoans(
        int id, [FromQuery] MemberLoansQuery query, CancellationToken cancellationToken)
    {
        await _members.GetMemberAsync(id, cancellationToken); // 404 if the member does not exist

        var loans = await _loans.GetLoansAsync(
            new LoanQuery { MemberId = id, Status = query.Status, Page = query.Page, PageSize = query.PageSize },
            cancellationToken);

        return Ok(loans);
    }
}

public sealed class MemberLoansQuery : PagedQuery
{
    public LibraryManagement.Domain.Loans.LoanStatus? Status { get; init; }
}
