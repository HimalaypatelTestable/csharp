using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Library.Model;

namespace Library.Repository;

public class AuthorRepository
{
    private readonly ConcurrentDictionary<string, Author> _authorsById = new();

    public Author Save(Author author)
    {
        ArgumentNullException.ThrowIfNull(author);
        _authorsById[author.Id] = author;
        return author;
    }

    public Author? FindById(string? id)
    {
        if (id is null) return null;
        return _authorsById.TryGetValue(id, out var a) ? a : null;
    }

    public IReadOnlyList<Author> FindAll() => _authorsById.Values.ToList();

    public IReadOnlyList<Author> FindByNameContains(string? fragment)
    {
        if (string.IsNullOrWhiteSpace(fragment))
            return Array.Empty<Author>();
        var needle = fragment.ToLowerInvariant();
        return _authorsById.Values
            .Where(a => a.DisplayName.ToLowerInvariant().Contains(needle)
                     || a.FullName.ToLowerInvariant().Contains(needle))
            .ToList();
    }

    public IReadOnlyList<Author> FindByNationality(string? nationality)
    {
        if (nationality is null) return Array.Empty<Author>();
        return _authorsById.Values
            .Where(a => string.Equals(a.Nationality, nationality, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public IReadOnlyList<Author> FindByGenre(string? genre)
    {
        if (genre is null) return Array.Empty<Author>();
        return _authorsById.Values
            .Where(a => a.WritesInGenre(genre))
            .ToList();
    }

    public IReadOnlyList<Author> FindLivingAuthors() =>
        _authorsById.Values.Where(a => a.IsAlive).ToList();

    public IReadOnlyList<Author> FindVerified() =>
        _authorsById.Values.Where(a => a.Verified).ToList();

    public IReadOnlyList<Author> FindMostProlific(int limit)
    {
        if (limit <= 0) return Array.Empty<Author>();
        return _authorsById.Values
            .OrderByDescending(a => a.BookCount)
            .Take(limit)
            .ToList();
    }

    public IReadOnlyList<Author> FindAwardWinners() =>
        _authorsById.Values.Where(a => a.Awards.Count > 0).ToList();

    public IReadOnlyList<Author> FindBornBetween(int fromYear, int toYear)
    {
        if (fromYear > toYear)
            throw new ArgumentException("fromYear must be <= toYear");
        return _authorsById.Values
            .Where(a => a.BirthDate is not null)
            .Where(a =>
            {
                var year = a.BirthDate!.Value.Year;
                return year >= fromYear && year <= toYear;
            })
            .ToList();
    }

    public bool DeleteById(string id) => _authorsById.TryRemove(id, out _);

    public int Count => _authorsById.Count;

    public int CountVerified() => _authorsById.Values.Count(a => a.Verified);

    public int CountLiving() => _authorsById.Values.Count(a => a.IsAlive);

    public bool ExistsById(string? id) =>
        id is not null && _authorsById.ContainsKey(id);

    public IReadOnlyList<string> DistinctNationalities() =>
        _authorsById.Values
            .Where(a => !string.IsNullOrWhiteSpace(a.Nationality))
            .Select(a => a.Nationality!)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

    public IReadOnlyList<string> DistinctGenres() =>
        _authorsById.Values
            .SelectMany(a => a.Genres)
            .Distinct()
            .OrderBy(g => g)
            .ToList();

    public IReadOnlyDictionary<string, int> CountByNationality()
    {
        return _authorsById.Values
            .Where(a => !string.IsNullOrWhiteSpace(a.Nationality))
            .GroupBy(a => a.Nationality!)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public void Clear() => _authorsById.Clear();
}
