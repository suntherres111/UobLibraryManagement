namespace LibraryManagement.Domain.Common;

/// <summary>Central list of business error codes returned to API clients.</summary>
public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string DuplicateIsbn = "DUPLICATE_ISBN";
    public const string DuplicateEmail = "DUPLICATE_EMAIL";
    public const string BookInactive = "BOOK_INACTIVE";
    public const string BookUnavailable = "BOOK_UNAVAILABLE";
    public const string BookHasActiveLoans = "BOOK_HAS_ACTIVE_LOANS";
    public const string CopiesBelowOnLoan = "COPIES_BELOW_ON_LOAN";
    public const string MemberInactive = "MEMBER_INACTIVE";
    public const string MemberHasActiveLoans = "MEMBER_HAS_ACTIVE_LOANS";
    public const string MemberHasOverdueLoans = "MEMBER_HAS_OVERDUE_LOANS";
    public const string LoanLimitReached = "LOAN_LIMIT_REACHED";
    public const string BookAlreadyBorrowed = "BOOK_ALREADY_BORROWED_BY_MEMBER";
    public const string LoanAlreadyReturned = "LOAN_ALREADY_RETURNED";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
}
