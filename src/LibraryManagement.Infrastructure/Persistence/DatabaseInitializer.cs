using LibraryManagement.Domain.Books;
using LibraryManagement.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Creates the database schema if it does not exist and optionally seeds sample data.
    /// For this assessment EnsureCreated keeps setup to one step; in production the schema
    /// would be managed with EF Core migrations (see README).
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));

        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (configuration.GetValue("Database:SeedSampleData", false) && !await db.Books.AnyAsync(cancellationToken))
        {
            db.Books.AddRange(
                new Book("9780132350884", "Clean Code", "Robert C. Martin", 2008, 3, "Prentice Hall", "Software Engineering"),
                new Book("9780201633610", "Design Patterns", "Erich Gamma et al.", 1994, 2, "Addison-Wesley", "Software Engineering"),
                new Book("9780321125217", "Domain-Driven Design", "Eric Evans", 2003, 1, "Addison-Wesley", "Software Architecture"),
                new Book("9781449373320", "Designing Data-Intensive Applications", "Martin Kleppmann", 2017, 2, "O'Reilly Media", "Software Architecture"),
                new Book("9780135957059", "The Pragmatic Programmer", "David Thomas, Andrew Hunt", 2019, 2, "Addison-Wesley", "Software Engineering"));

            db.Members.AddRange(
                new Member("Ahmad", "Razak", "ahmad.razak@example.com", "+60123456789", MembershipType.Standard),
                new Member("Mei Ling", "Tan", "meiling.tan@example.com", "+60198765432", MembershipType.Premium),
                new Member("Priya", "Nair", "priya.nair@example.com", null, MembershipType.Standard));

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded sample books and members.");
        }
    }
}
