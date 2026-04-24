using System;
using System.Collections.Generic;
using System.Linq;

namespace Library.Model;

public class Book
{
    public enum BookStatus
    {
        Available,
        Borrowed,
        Reserved,
        Lost,
        UnderRepair,
        Archived
    }

    private readonly List<Author> _authors = new();
    private string _isbn = string.Empty;
    private string _title = string.Empty;
    private int _numberOfCopies = 1;
    private int _availableCopies = 1;

    public Book(string id, string isbn, string title)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Book id cannot be null or blank", nameof(id));
        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN cannot be null or blank", nameof(isbn));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or blank", nameof(title));

        Id = id;
        _isbn = isbn;
        _title = title;
        AddedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        Status = BookStatus.Available;
        Language = "English";
    }

    public string Id { get; }

    public string Isbn
    {
        get => _isbn;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("ISBN cannot be null or blank", nameof(value));
            _isbn = value;
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Title cannot be null or blank", nameof(value));
            _title = value;
        }
    }

    public IReadOnlyList<Author> Authors => _authors.AsReadOnly();

    public Publisher? Publisher { get; set; }

    public Category? Category { get; set; }

    public int PublicationYear { get; set; }

    public int NumberOfCopies
    {
        get => _numberOfCopies;
        set
        {
            if (value < 0)
                throw new ArgumentException("Number of copies cannot be negative", nameof(value));
            var borrowed = _numberOfCopies - _availableCopies;
            _numberOfCopies = value;
            _availableCopies = Math.Max(0, value - borrowed);
        }
    }

    public int AvailableCopies => _availableCopies;

    public string Language { get; set; }

    public int PageCount { get; set; }

    public double Price { get; set; }

    public DateOnly AddedDate { get; }

    public BookStatus Status { get; set; }

    public string? ShelfLocation { get; set; }

    public double AverageRating { get; private set; }

    public int RatingCount { get; private set; }

    public void AddAuthor(Author author)
    {
        ArgumentNullException.ThrowIfNull(author);
        if (!_authors.Contains(author))
            _authors.Add(author);
    }

    public bool RemoveAuthor(Author author) => _authors.Remove(author);

    public void AddRating(double rating)
    {
        if (rating < 0 || rating > 5)
            throw new ArgumentException("Rating must be between 0 and 5", nameof(rating));
        var total = AverageRating * RatingCount + rating;
        RatingCount += 1;
        AverageRating = total / RatingCount;
    }

    public bool IsAvailable => _availableCopies > 0 && Status == BookStatus.Available;

    public bool Borrow()
    {
        if (!IsAvailable)
            return false;
        _availableCopies -= 1;
        if (_availableCopies == 0)
            Status = BookStatus.Borrowed;
        return true;
    }

    public bool ReturnCopy()
    {
        if (_availableCopies >= _numberOfCopies)
            return false;
        _availableCopies += 1;
        if (_availableCopies > 0 && Status == BookStatus.Borrowed)
            Status = BookStatus.Available;
        return true;
    }

    public int BorrowedCopies => _numberOfCopies - _availableCopies;

    public bool HasAuthor(string authorId)
    {
        if (string.IsNullOrWhiteSpace(authorId))
            return false;
        return _authors.Any(a => a.Id == authorId);
    }

    public bool MatchesTitleFragment(string fragment)
    {
        if (string.IsNullOrWhiteSpace(fragment))
            return false;
        return _title.Contains(fragment, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        if (obj is Book other)
            return Id == other.Id;
        return false;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString()
        => $"Book{{Id='{Id}', Title='{_title}', Isbn='{_isbn}', Available={_availableCopies}/{_numberOfCopies}}}";
}
