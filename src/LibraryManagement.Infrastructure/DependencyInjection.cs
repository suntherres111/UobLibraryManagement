using LibraryManagement.Application.Abstractions;
using LibraryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagement.Infrastructure;

public static class DependencyInjection
{
    public const string SqlServerProvider = "SqlServer";
    public const string SqliteProvider = "Sqlite";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Configuration is read when the DbContext is first built (not at registration),
        // so integration tests can override the provider/connection string.
        services.AddDbContext<LibraryDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var provider = configuration["Database:Provider"] ?? SqlServerProvider;

            if (string.Equals(provider, SqliteProvider, StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(GetConnectionString(configuration, SqliteProvider));
            }
            else
            {
                options.UseSqlServer(GetConnectionString(configuration, SqlServerProvider));
            }
        });

        services.AddScoped<ILibraryDbContext>(sp => sp.GetRequiredService<LibraryDbContext>());

        return services;
    }

    private static string GetConnectionString(IConfiguration configuration, string name)
        => configuration.GetConnectionString(name)
           ?? throw new InvalidOperationException($"Connection string '{name}' is not configured.");
}
