using LibraryManagement.Application.Abstractions;
using LibraryManagement.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Application.Common;

internal static class DbContextExtensions
{
    /// <summary>
    /// Saves changes and converts an optimistic-concurrency failure into a business error.
    /// Book.AvailableCopies is a concurrency token, so two requests borrowing the last copy
    /// at the same time cannot both succeed: the second one gets HTTP 409 and can retry.
    /// </summary>
    public static async Task SaveChangesWithConcurrencyCheckAsync(this ILibraryDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException(
                ErrorCodes.ConcurrencyConflict,
                "The record was changed by another request. Please reload and try again.");
        }
    }
}
