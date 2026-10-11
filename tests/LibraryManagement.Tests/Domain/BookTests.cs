using LibraryManagement.Domain.Books;
using LibraryManagement.Domain.Common;
using LibraryManagement.Domain.Lending;
using LibraryManagement.Tests.Support;

namespace LibraryManagement.Tests.Domain;

public sealed class BookTests
{
    [Theory]
    [InlineData("978-0-13-235088-4", "9780132350884")]
    [InlineData("0 13 235088 2", "0132350882")]
    [InlineData("080442957x", "080442957X")]
    public void NormalizeIsbn_removes_separators(string input, string expected)
        => Assert.Equal(expected, Book.NormalizeIsbn(input));

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("97801323508AB")]
    public void Constructor_rejects_invalid_isbn(string isbn)
        => Assert.Throws<DomainValidationException>(() => new Book(isbn, "Title", "Author", 2000, 1));

    [Fact]
    public void Constructor_requires_at_least_one_copy()
        => Assert.Throws<DomainValidationException>(() => new Book("9780132350884", "Title", "Author", 2000, 0));

    [Fact]
    public void New_book_has_all_copies_available()
    {
        var book = new Book("9780132350884", "  Clean Code  ", "Robert C. Martin", 2008, 3);

        Assert.Equal("Clean Code", book.Title);
        Assert.Equal(3, book.AvailableCopies);
        Assert.True(book.IsAvailable);
    }

    [Fact]
    public void ChangeTotalCopies_keeps_copies_on_loan()
    {
        var clock = new TestClock();
        var book = TestData.Book(copies: 3);
        new Library(clock).CheckOut(book, TestData.Member(), []);

        book.ChangeTotalCopies(5);

        Assert.Equal(5, book.TotalCopies);
        Assert.Equal(4, book.AvailableCopies);
        Assert.Equal(1, book.CopiesOnLoan);
    }

    [Fact]
    public void ChangeTotalCopies_cannot_go_below_copies_on_loan()
    {
        var clock = new TestClock();
        var library = new Library(clock);
        var book = TestData.Book(copies: 2);
        library.CheckOut(book, TestData.Member(), []);
        library.CheckOut(book, TestData.Member(), []);

        var ex = Assert.Throws<DomainException>(() => book.ChangeTotalCopies(1));
        Assert.Equal(ErrorCodes.CopiesBelowOnLoan, ex.Code);
    }

    [Fact]
    public void Retire_fails_while_copies_are_on_loan()
    {
        var book = TestData.Book();
        new Library(new TestClock()).CheckOut(book, TestData.Member(), []);

        var ex = Assert.Throws<DomainException>(book.Retire);
        Assert.Equal(ErrorCodes.BookHasActiveLoans, ex.Code);
    }
}
