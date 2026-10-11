using LibraryManagement.Application.Abstractions;
using LibraryManagement.Application.Common;
using LibraryManagement.Domain.Common;
using LibraryManagement.Domain.Members;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Application.Members;

public interface IMemberService
{
    Task<PagedResult<MemberResponse>> GetMembersAsync(MemberQuery query, CancellationToken cancellationToken = default);

    Task<MemberResponse> GetMemberAsync(int id, CancellationToken cancellationToken = default);

    Task<MemberResponse> CreateMemberAsync(CreateMemberRequest request, CancellationToken cancellationToken = default);

    Task<MemberResponse> UpdateMemberAsync(int id, UpdateMemberRequest request, CancellationToken cancellationToken = default);

    Task DeactivateMemberAsync(int id, CancellationToken cancellationToken = default);

    Task<MemberResponse> ReactivateMemberAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class MemberService : IMemberService
{
    private readonly ILibraryDbContext _db;

    public MemberService(ILibraryDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<MemberResponse>> GetMembersAsync(MemberQuery query, CancellationToken cancellationToken = default)
    {
        var members = _db.Members.AsNoTracking();

        if (!query.IncludeInactive)
        {
            members = members.Where(m => m.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            members = members.Where(m =>
                m.FirstName.Contains(term) || m.LastName.Contains(term) || m.Email.Contains(term.ToLower()));
        }

        if (query.MembershipType.HasValue)
        {
            members = members.Where(m => m.MembershipType == query.MembershipType.Value);
        }

        var (items, total) = await members
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ThenBy(m => m.Id)
            .ToPageAsync(query, cancellationToken);

        return new PagedResult<MemberResponse>(items.Select(m => m.ToResponse()).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<MemberResponse> GetMemberAsync(int id, CancellationToken cancellationToken = default)
    {
        var member = await _db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Member), id);

        return member.ToResponse();
    }

    public async Task<MemberResponse> CreateMemberAsync(CreateMemberRequest request, CancellationToken cancellationToken = default)
    {
        var member = new Member(request.FirstName, request.LastName, request.Email, request.PhoneNumber, request.MembershipType);

        await EnsureEmailIsUniqueAsync(member.Email, excludeMemberId: null, cancellationToken);

        _db.Members.Add(member);
        await _db.SaveChangesAsync(cancellationToken);

        return member.ToResponse();
    }

    public async Task<MemberResponse> UpdateMemberAsync(int id, UpdateMemberRequest request, CancellationToken cancellationToken = default)
    {
        var member = await FindAsync(id, cancellationToken);

        member.UpdateDetails(request.FirstName, request.LastName, request.Email, request.PhoneNumber);
        member.ChangeMembershipType(request.MembershipType);

        await EnsureEmailIsUniqueAsync(member.Email, excludeMemberId: id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return member.ToResponse();
    }

    public async Task DeactivateMemberAsync(int id, CancellationToken cancellationToken = default)
    {
        var member = await FindAsync(id, cancellationToken);

        var activeLoans = await _db.Loans.CountAsync(l => l.MemberId == id && l.ReturnedAtUtc == null, cancellationToken);
        member.Deactivate(activeLoans);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MemberResponse> ReactivateMemberAsync(int id, CancellationToken cancellationToken = default)
    {
        var member = await FindAsync(id, cancellationToken);

        member.Reactivate();
        await _db.SaveChangesAsync(cancellationToken);

        return member.ToResponse();
    }

    private async Task EnsureEmailIsUniqueAsync(string email, int? excludeMemberId, CancellationToken cancellationToken)
    {
        var exists = await _db.Members.AnyAsync(
            m => m.Email == email && (excludeMemberId == null || m.Id != excludeMemberId),
            cancellationToken);

        if (exists)
        {
            throw new DomainException(ErrorCodes.DuplicateEmail, $"A member with email {email} already exists.");
        }
    }

    private async Task<Member> FindAsync(int id, CancellationToken cancellationToken)
        => await _db.Members.FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
           ?? throw new NotFoundException(nameof(Member), id);
}
