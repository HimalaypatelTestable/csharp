using Library.Model;
using Xunit;

namespace Library.Tests;

public class SmokeTests
{
    [Fact]
    public void Book_Constructor_StoresId()
    {
        var book = new Book("b1", "978-0123456789", "Clean Code");
        Assert.Equal("b1", book.Id);
    }

    [Fact]
    public void Book_Constructor_StoresTitle()
    {
        var book = new Book("b2", "978-1234567890", "The Pragmatic Programmer");
        Assert.Equal("The Pragmatic Programmer", book.Title);
    }

    [Fact]
    public void Book_DefaultStatus_IsAvailable()
    {
        var book = new Book("b3", "978-1111222233", "Refactoring");
        Assert.Equal(Book.BookStatus.Available, book.Status);
    }
}
