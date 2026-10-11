using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LibraryManagement.Application.Books;
using LibraryManagement.Application.Loans;
using LibraryManagement.Application.Members;
using LibraryManagement.Domain.Loans;
using LibraryManagement.Tests.Support;

namespace LibraryManagement.Tests.Api;

/// <summary>End-to-end tests through HTTP: routing, validation, error mapping and persistence.</summary>
public sealed class LibraryApiTests : IClassFixture<LibraryApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public LibraryApiTests(LibraryApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Borrow_and_return_flow()
    {
        var book = await CreateBookAsync(copies: 1);
        var alice = await CreateMemberAsync();
        var bob = await CreateMemberAsync();

        // Alice borrows the only copy.
        var borrow = await _client.PostAsJsonAsync("/api/loans", new { bookId = book.Id, memberId = alice.Id });
        Assert.Equal(HttpStatusCode.Created, borrow.StatusCode);
        var loan = (await borrow.Content.ReadFromJsonAsync<LoanResponse>(Json))!;
        Assert.Equal(LoanStatus.Active, loan.Status);

        var afterBorrow = await _client.GetFromJsonAsync<BookResponse>($"/api/books/{book.Id}", Json);
        Assert.Equal(0, afterBorrow!.AvailableCopies);

        // Bob cannot borrow it while it is out.
        var blocked = await _client.PostAsJsonAsync("/api/loans", new { bookId = book.Id, memberId = bob.Id });
        await AssertProblemAsync(blocked, HttpStatusCode.Conflict, "BOOK_UNAVAILABLE");

        // Alice returns it.
        var returned = await _client.PostAsync($"/api/loans/{loan.Id}/return", content: null);
        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);
        Assert.Equal(LoanStatus.Returned, (await returned.Content.ReadFromJsonAsync<LoanResponse>(Json))!.Status);

        // Returning twice is rejected.
        var again = await _client.PostAsync($"/api/loans/{loan.Id}/return", content: null);
        await AssertProblemAsync(again, HttpStatusCode.Conflict, "LOAN_ALREADY_RETURNED");

        var history = await _client.GetFromJsonAsync<JsonElement>($"/api/members/{alice.Id}/loans", Json);
        Assert.Equal(1, history.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Create_book_with_missing_title_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/books", new
        {
            isbn = TestData.UniqueIsbn(),
            title = "",
            author = "Someone",
            publishedYear = 2020,
            totalCopies = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_book_with_invalid_isbn_returns_400_with_code()
    {
        var response = await _client.PostAsJsonAsync("/api/books", new
        {
            isbn = "978-ABC-123456",
            title = "Title",
            author = "Author",
            publishedYear = 2020,
            totalCopies = 1
        });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
    }

    [Fact]
    public async Task Duplicate_isbn_returns_409()
    {
        var book = await CreateBookAsync(copies: 1);

        var response = await _client.PostAsJsonAsync("/api/books", new
        {
            isbn = book.Isbn,
            title = "Another",
            author = "Author",
            publishedYear = 2020,
            totalCopies = 1
        });

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "DUPLICATE_ISBN");
    }

    [Fact]
    public async Task Unknown_book_returns_404()
    {
        var response = await _client.GetAsync("/api/books/987654");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "NOT_FOUND");
    }

    [Fact]
    public async Task Member_with_books_on_loan_cannot_be_deactivated()
    {
        var book = await CreateBookAsync(copies: 1);
        var member = await CreateMemberAsync();
        (await _client.PostAsJsonAsync("/api/loans", new { bookId = book.Id, memberId = member.Id })).EnsureSuccessStatusCode();

        var response = await _client.DeleteAsync($"/api/members/{member.Id}");

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "MEMBER_HAS_ACTIVE_LOANS");
    }

    private async Task<BookResponse> CreateBookAsync(int copies)
    {
        var response = await _client.PostAsJsonAsync("/api/books", new
        {
            isbn = TestData.UniqueIsbn(),
            title = "Clean Code",
            author = "Robert C. Martin",
            publishedYear = 2008,
            totalCopies = copies
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<BookResponse>(Json))!;
    }

    private async Task<MemberResponse> CreateMemberAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/members", new
        {
            firstName = "Test",
            lastName = "Member",
            email = $"member-{Guid.NewGuid():N}@example.com",
            membershipType = "Standard"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MemberResponse>(Json))!;
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus, string expectedCode)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
    }
}
