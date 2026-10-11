using LibraryManagement.Domain.Books;
using LibraryManagement.Domain.Common;
using LibraryManagement.Domain.Members;

namespace LibraryManagement.Tests.Support;

internal static class TestData
{
    private static int _nextId = 1000;

    public static Book Book(int copies = 1, string? isbn = null)
        => WithId(new Book(isbn ?? UniqueIsbn(), "Clean Code", "Robert C. Martin", 2008, copies));

    public static Member Member(MembershipType type = MembershipType.Standard)
        => WithId(new Member("Ahmad", "Razak", $"member{Interlocked.Increment(ref _nextId)}@example.com", null, type));

    public static string UniqueIsbn() => $"978{Random.Shared.NextInt64(1_000_000_000, 9_999_999_999)}";

    /// <summary>Unit tests run without a database, so give entities an Id like EF would.</summary>
    private static T WithId<T>(T entity) where T : Entity
    {
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, Interlocked.Increment(ref _nextId));
        return entity;
    }
}
