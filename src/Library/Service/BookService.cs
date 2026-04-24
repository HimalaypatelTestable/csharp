using System;
using System.Collections.Generic;
using System.Linq;
using Library.Exceptions;
using Library.Model;
using Library.Repository;
using Library.Util;

namespace Library.Service;

public class BookService
{
    private readonly BookRepository _bookRepository;
    private readonly AuthorRepository _authorRepository;
    private readonly IdGenerator _idGenerator;

    public BookService(BookRepository bookRepository,
                        AuthorRepository authorRepository,
                        IdGenerator idGenerator)
    {
        _bookRepository = bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));
        _authorRepository = authorRepository ?? throw new ArgumentNullException(nameof(authorRepository));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
    }

    public Book CreateBook(string isbn, string title, int publicationYear, int numberOfCopies)
    {
        ValidationUtils.RequireIsbn(isbn);
        ValidationUtils.RequireNonBlank(title, nameof(title));
        if (numberOfCopies < 1)
            throw new ArgumentException("Number of copies must be at least 1", nameof(numberOfCopies));
        if (_bookRepository.ExistsByIsbn(isbn))
            throw new LibraryException($"Book with ISBN already exists: {isbn}");

        var id = _idGenerator.NextBookId();
        var book = new Book(id, isbn, title)
        {
            PublicationYear = publicationYear,
            NumberOfCopies = numberOfCopies
        };
        return _bookRepository.Save(book);
    }

    public Book UpdateTitle(string bookId, string newTitle)
    {
        var book = RequireBook(bookId);
        ValidationUtils.RequireNonBlank(newTitle, nameof(newTitle));
        book.Title = newTitle;
        return _bookRepository.Save(book);
    }

    public Book AssignAuthor(string bookId, string authorId)
    {
        var book = RequireBook(bookId);
        var author = _authorRepository.FindById(authorId)
            ?? throw new LibraryException($"Author not found: {authorId}");
        book.AddAuthor(author);
        author.AddBookId(book.Id);
        _bookRepository.Save(book);
        _authorRepository.Save(author);
        return book;
    }

    public Book UnassignAuthor(string bookId, string authorId)
    {
        var book = RequireBook(bookId);
        var author = _authorRepository.FindById(authorId)
            ?? throw new LibraryException($"Author not found: {authorId}");
        book.RemoveAuthor(author);
        author.RemoveBookId(book.Id);
        _bookRepository.Save(book);
        _authorRepository.Save(author);
        return book;
    }

    public Book AssignCategory(string bookId, Category? category)
    {
        var book = RequireBook(bookId);
        book.Category?.RemoveBookId(book.Id);
        book.Category = category;
        category?.AddBookId(book.Id);
        return _bookRepository.Save(book);
    }

    public Book AssignPublisher(string bookId, Publisher? publisher)
    {
        var book = RequireBook(bookId);
        if (book.Publisher is not null && publisher is not null
            && book.Publisher.Id != publisher.Id)
            book.Publisher.RemoveBookId(book.Id);
        book.Publisher = publisher;
        publisher?.AddBookId(book.Id);
        return _bookRepository.Save(book);
    }

    public Book AddCopies(string bookId, int extraCopies)
    {
        if (extraCopies <= 0)
            throw new ArgumentException("Extra copies must be positive", nameof(extraCopies));
        var book = RequireBook(bookId);
        book.NumberOfCopies += extraCopies;
        return _bookRepository.Save(book);
    }

    public Book RemoveCopies(string bookId, int copiesToRemove)
    {
        if (copiesToRemove <= 0)
            throw new ArgumentException("Copies to remove must be positive", nameof(copiesToRemove));
        var book = RequireBook(bookId);
        var borrowed = book.BorrowedCopies;
        var newTotal = book.NumberOfCopies - copiesToRemove;
        if (newTotal < borrowed)
            throw new LibraryException("Cannot remove copies currently on loan");
        book.NumberOfCopies = newTotal;
        return _bookRepository.Save(book);
    }

    public Book RateBook(string bookId, double rating)
    {
        var book = RequireBook(bookId);
        book.AddRating(rating);
        return _bookRepository.Save(book);
    }

    public Book MarkLost(string bookId)
    {
        var book = RequireBook(bookId);
        book.Status = Book.BookStatus.Lost;
        return _bookRepository.Save(book);
    }

    public Book SendForRepair(string bookId)
    {
        var book = RequireBook(bookId);
        book.Status = Book.BookStatus.UnderRepair;
        return _bookRepository.Save(book);
    }

    public Book RestoreToShelf(string bookId)
    {
        var book = RequireBook(bookId);
        book.Status = Book.BookStatus.Available;
        return _bookRepository.Save(book);
    }

    public void DeleteBook(string bookId)
    {
        var book = RequireBook(bookId);
        foreach (var author in book.Authors.ToList())
            author.RemoveBookId(book.Id);
        book.Category?.RemoveBookId(book.Id);
        book.Publisher?.RemoveBookId(book.Id);
        _bookRepository.DeleteById(bookId);
    }

    public Book? FindById(string bookId) => _bookRepository.FindById(bookId);

    public IReadOnlyList<Book> FindAll() => _bookRepository.FindAll();

    public IReadOnlyList<Book> FindAvailable() => _bookRepository.FindAvailable();

    public IReadOnlyList<Book> FindLowStock(int threshold)
    {
        if (threshold < 0)
            throw new ArgumentException("Threshold cannot be negative", nameof(threshold));
        return _bookRepository.FindAll()
            .Where(b => b.AvailableCopies <= threshold)
            .ToList();
    }

    private Book RequireBook(string bookId)
    {
        return _bookRepository.FindById(bookId)
            ?? throw new LibraryException($"Book not found: {bookId}");
    }
}
