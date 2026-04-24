using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Library.Model;

namespace Library.Repository;

public class BookRepository
{
    private readonly ConcurrentDictionary<string, Book> _booksById = new();
    private readonly ConcurrentDictionary<string, string> _isbnIndex = new();
    private readonly Dictionary<string, List<string>> _titleIndex = new();
    private readonly Dictionary<string, List<string>> _categoryIndex = new();
    private readonly Dictionary<string, List<string>> _authorIndex = new();
    private readonly object _indexLock = new();

    public Book Save(Book book)
    {
        ArgumentNullException.ThrowIfNull(book);

        if (_booksById.TryGetValue(book.Id, out var existing) && existing.Isbn != book.Isbn)
            _isbnIndex.TryRemove(existing.Isbn, out _);

        _booksById[book.Id] = book;
        _isbnIndex[book.Isbn] = book.Id;
        IndexBook(book);
        return book;
    }

    private void IndexBook(Book book)
    {
        lock (_indexLock)
        {
            var titleKey = Normalize(book.Title);
            if (!_titleIndex.TryGetValue(titleKey, out var titleList))
            {
                titleList = new List<string>();
                _titleIndex[titleKey] = titleList;
            }
            if (!titleList.Contains(book.Id))
                titleList.Add(book.Id);

            if (book.Category is not null)
            {
                var catKey = book.Category.Id;
                if (!_categoryIndex.TryGetValue(catKey, out var catList))
                {
                    catList = new List<string>();
                    _categoryIndex[catKey] = catList;
                }
                if (!catList.Contains(book.Id))
                    catList.Add(book.Id);
            }

            foreach (var author in book.Authors)
            {
                if (!_authorIndex.TryGetValue(author.Id, out var list))
                {
                    list = new List<string>();
                    _authorIndex[author.Id] = list;
                }
                if (!list.Contains(book.Id))
                    list.Add(book.Id);
            }
        }
    }

    private static string Normalize(string value) =>
        value is null ? string.Empty : value.ToLowerInvariant().Trim();

    public Book? FindById(string? id)
    {
        if (id is null) return null;
        return _booksById.TryGetValue(id, out var b) ? b : null;
    }

    public Book? FindByIsbn(string? isbn)
    {
        if (isbn is null) return null;
        return _isbnIndex.TryGetValue(isbn, out var id) ? FindById(id) : null;
    }

    public IReadOnlyList<Book> FindAll() => _booksById.Values.ToList();

    public IReadOnlyList<Book> FindByTitleContains(string? fragment)
    {
        if (string.IsNullOrWhiteSpace(fragment))
            return Array.Empty<Book>();
        var needle = Normalize(fragment);
        return _booksById.Values
            .Where(b => Normalize(b.Title).Contains(needle))
            .ToList();
    }

    public IReadOnlyList<Book> FindByCategory(Category? category)
    {
        if (category is null) return Array.Empty<Book>();
        lock (_indexLock)
        {
            if (!_categoryIndex.TryGetValue(category.Id, out var ids))
                return Array.Empty<Book>();
            return ids.Select(FindById).Where(b => b is not null).Cast<Book>().ToList();
        }
    }

    public IReadOnlyList<Book> FindByAuthorId(string? authorId)
    {
        if (authorId is null) return Array.Empty<Book>();
        lock (_indexLock)
        {
            if (!_authorIndex.TryGetValue(authorId, out var ids))
                return Array.Empty<Book>();
            return ids.Select(FindById).Where(b => b is not null).Cast<Book>().ToList();
        }
    }

    public IReadOnlyList<Book> FindAvailable() =>
        _booksById.Values.Where(b => b.IsAvailable).ToList();

    public IReadOnlyList<Book> FindByStatus(Book.BookStatus status) =>
        _booksById.Values.Where(b => b.Status == status).ToList();

    public IReadOnlyList<Book> FindByPublicationYearRange(int fromYear, int toYear)
    {
        if (fromYear > toYear)
            throw new ArgumentException("fromYear must be <= toYear");
        return _booksById.Values
            .Where(b => b.PublicationYear >= fromYear && b.PublicationYear <= toYear)
            .ToList();
    }

    public IReadOnlyList<Book> FindTopRated(int limit)
    {
        if (limit <= 0) return Array.Empty<Book>();
        return _booksById.Values
            .Where(b => b.RatingCount > 0)
            .OrderByDescending(b => b.AverageRating)
            .Take(limit)
            .ToList();
    }

    public bool DeleteById(string id)
    {
        if (!_booksById.TryRemove(id, out var removed))
            return false;
        _isbnIndex.TryRemove(removed.Isbn, out _);
        lock (_indexLock)
        {
            var titleKey = Normalize(removed.Title);
            if (_titleIndex.TryGetValue(titleKey, out var titleList))
                titleList.Remove(id);
            if (removed.Category is not null
                && _categoryIndex.TryGetValue(removed.Category.Id, out var catList))
                catList.Remove(id);
            foreach (var author in removed.Authors)
                if (_authorIndex.TryGetValue(author.Id, out var list))
                    list.Remove(id);
        }
        return true;
    }

    public int Count => _booksById.Count;

    public int CountAvailable() => _booksById.Values.Count(b => b.IsAvailable);

    public bool ExistsById(string? id) => id is not null && _booksById.ContainsKey(id);

    public bool ExistsByIsbn(string? isbn) => isbn is not null && _isbnIndex.ContainsKey(isbn);

    public void Clear()
    {
        _booksById.Clear();
        _isbnIndex.Clear();
        lock (_indexLock)
        {
            _titleIndex.Clear();
            _categoryIndex.Clear();
            _authorIndex.Clear();
        }
    }
}
