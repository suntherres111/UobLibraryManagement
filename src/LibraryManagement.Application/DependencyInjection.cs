using LibraryManagement.Application.Books;
using LibraryManagement.Application.Loans;
using LibraryManagement.Application.Members;
using LibraryManagement.Domain.Lending;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LibraryManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // TimeProvider (built into .NET 8) is injected so tests can control "now".
        services.TryAddSingleton(TimeProvider.System);

        // Stateless domain service: safe as a singleton.
        services.AddSingleton<Library>();

        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<ILoanService, LoanService>();

        return services;
    }
}
