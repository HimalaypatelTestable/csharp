using System;
using System.Collections.Generic;
using System.Linq;

namespace Library.Model;

public class Publisher
{
    private readonly List<string> _bookIds = new();
    private readonly List<string> _specialties = new();
    private string _name;
    private string? _email;

    public Publisher(string id, string name)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Publisher id cannot be null or blank", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Publisher name cannot be null or blank", nameof(name));

        Id = id;
        _name = name;
        Active = true;
    }

    public string Id { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Name cannot be null or blank", nameof(value));
            _name = value;
        }
    }

    public string? Country { get; set; }

    public string? City { get; set; }

    public string? Address { get; set; }

    public string? Email
    {
        get => _email;
        set
        {
            if (value is not null && !value.Contains('@'))
                throw new ArgumentException("Invalid email", nameof(value));
            _email = value;
        }
    }

    public string? Phone { get; set; }

    public string? Website { get; set; }

    public int FoundedYear { get; private set; }

    public bool Active { get; set; }

    public string? TaxId { get; set; }

    public double AverageBookPrice { get; set; }

    public IReadOnlyList<string> BookIds => _bookIds.AsReadOnly();

    public IReadOnlyList<string> Specialties => _specialties.AsReadOnly();

    public int BookCount => _bookIds.Count;

    public int YearsInBusiness
    {
        get
        {
            if (FoundedYear <= 0) return 0;
            return DateTime.UtcNow.Year - FoundedYear;
        }
    }

    public string Location
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(City)) parts.Add(City!);
            if (!string.IsNullOrWhiteSpace(Country)) parts.Add(Country!);
            return string.Join(", ", parts);
        }
    }

    public void SetFoundedYear(int year)
    {
        var currentYear = DateTime.UtcNow.Year;
        if (year < 1400 || year > currentYear)
            throw new ArgumentException($"Invalid founded year: {year}", nameof(year));
        FoundedYear = year;
    }

    public void AddBookId(string bookId)
    {
        if (string.IsNullOrWhiteSpace(bookId))
            throw new ArgumentException("Book id cannot be null or blank", nameof(bookId));
        if (!_bookIds.Contains(bookId))
            _bookIds.Add(bookId);
    }

    public bool RemoveBookId(string bookId) => _bookIds.Remove(bookId);

    public void AddSpecialty(string specialty)
    {
        if (!string.IsNullOrWhiteSpace(specialty) && !_specialties.Contains(specialty))
            _specialties.Add(specialty);
    }

    public bool RemoveSpecialty(string specialty) => _specialties.Remove(specialty);

    public bool HasSpecialty(string? specialty)
    {
        if (string.IsNullOrWhiteSpace(specialty))
            return false;
        return _specialties.Any(s => string.Equals(s, specialty, StringComparison.OrdinalIgnoreCase));
    }

    public void SetAverageBookPrice(double price)
    {
        if (price < 0)
            throw new ArgumentException("Average price cannot be negative", nameof(price));
        AverageBookPrice = price;
    }

    public void Activate() => Active = true;

    public void Deactivate() => Active = false;

    public override bool Equals(object? obj) => obj is Publisher p && p.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString()
        => $"Publisher{{Id='{Id}', Name='{_name}', Location='{Location}', Books={_bookIds.Count}, Active={Active}}}";
}
