using System;
using System.Collections.Generic;
using System.Linq;

namespace Library.Model;

public class Author
{
    private readonly List<string> _bookIds = new();
    private readonly List<string> _awards = new();
    private readonly List<string> _genres = new();
    private string _firstName;
    private string _lastName;

    public Author(string id, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Author id cannot be null or blank", nameof(id));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be null or blank", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be null or blank", nameof(lastName));

        Id = id;
        _firstName = firstName;
        _lastName = lastName;
    }

    public string Id { get; }

    public string FirstName
    {
        get => _firstName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("First name cannot be null or blank", nameof(value));
            _firstName = value;
        }
    }

    public string LastName
    {
        get => _lastName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Last name cannot be null or blank", nameof(value));
            _lastName = value;
        }
    }

    public string FullName => $"{_firstName} {_lastName}";

    public string DisplayName => string.IsNullOrWhiteSpace(PenName) ? FullName : PenName!;

    public string? PenName { get; set; }

    public string? Nationality { get; set; }

    public DateOnly? BirthDate { get; set; }

    public DateOnly? DeathDate { get; set; }

    public string? Biography { get; set; }

    public string? Website { get; set; }

    public bool Verified { get; set; }

    public IReadOnlyList<string> BookIds => _bookIds.AsReadOnly();

    public IReadOnlyList<string> Awards => _awards.AsReadOnly();

    public IReadOnlyList<string> Genres => _genres.AsReadOnly();

    public bool IsAlive => DeathDate is null;

    public int Age
    {
        get
        {
            if (BirthDate is null) return 0;
            var endDate = DeathDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var age = endDate.Year - BirthDate.Value.Year;
            if (BirthDate.Value > endDate.AddYears(-age)) age -= 1;
            return age;
        }
    }

    public int BookCount => _bookIds.Count;

    public void AddBookId(string bookId)
    {
        if (string.IsNullOrWhiteSpace(bookId))
            throw new ArgumentException("Book id cannot be null or blank", nameof(bookId));
        if (!_bookIds.Contains(bookId))
            _bookIds.Add(bookId);
    }

    public bool RemoveBookId(string bookId) => _bookIds.Remove(bookId);

    public void AddAward(string award)
    {
        if (!string.IsNullOrWhiteSpace(award) && !_awards.Contains(award))
            _awards.Add(award);
    }

    public void AddGenre(string genre)
    {
        if (!string.IsNullOrWhiteSpace(genre) && !_genres.Contains(genre))
            _genres.Add(genre);
    }

    public bool WritesInGenre(string? genre)
    {
        if (string.IsNullOrWhiteSpace(genre))
            return false;
        return _genres.Any(g => string.Equals(g, genre, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasPublishedIn(int year)
    {
        if (BirthDate is null) return false;
        if (year < BirthDate.Value.Year) return false;
        if (DeathDate is not null && year > DeathDate.Value.Year) return false;
        return true;
    }

    public void SetDeathDate(DateOnly? deathDate)
    {
        if (deathDate is not null && BirthDate is not null && deathDate < BirthDate)
            throw new ArgumentException("Death date cannot be before birth date", nameof(deathDate));
        DeathDate = deathDate;
    }

    public void SetBirthDate(DateOnly? birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (birthDate is not null && birthDate > today)
            throw new ArgumentException("Birth date cannot be in the future", nameof(birthDate));
        BirthDate = birthDate;
    }

    public override bool Equals(object? obj) => obj is Author a && a.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString()
        => $"Author{{Id='{Id}', Name='{DisplayName}', Books={_bookIds.Count}, Verified={Verified}}}";
}
