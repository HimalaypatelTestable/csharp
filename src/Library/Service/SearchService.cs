using System;
using System.Collections.Generic;
using System.Linq;
using Library.Model;
using Library.Repository;

namespace Library.Service;

public class SearchService
{
    public class BookFilter
    {
        public string? TitleFragment { get; set; }
        public string? AuthorId { get; set; }
        public string? CategoryId { get; set; }
        public string? Language { get; set; }
        public int? FromYear { get; set; }
        public int? ToYear { get; set; }
        public bool? OnlyAvailable { get; set; }
        public double? MinRating { get; set; }

        public BookFilter WithTitle(string? v) { TitleFragment = v; return this; }
        public BookFilter WithAuthor(string? v) { AuthorId = v; return this; }
        public BookFilter WithCategory(string? v) { CategoryId = v; return this; }
        public BookFilter WithLanguage(string? v) { Language = v; return this; }
        public BookFilter WithFromYear(int? v) { FromYear = v; return this; }
        public BookFilter WithToYear(int? v) { ToYear = v; return this; }
        public BookFilter WithAvailable(bool? v) { OnlyAvailable = v; return this; }
        public BookFilter WithMinRating(double? v) { MinRating = v; return this; }
    }

    public enum BookSort
    {
        TitleAsc,
        TitleDesc,
        YearAsc,
        YearDesc,
        RatingDesc,
        AvailabilityDesc
    }

    private readonly BookRepository _bookRepository;
    private readonly AuthorRepository _authorRepository;
    private readonly MemberRepository _memberRepository;

    public SearchService(BookRepository bookRepository,
                         AuthorRepository authorRepository,
                         MemberRepository memberRepository)
    {
        _bookRepository = bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));
        _authorRepository = authorRepository ?? throw new ArgumentNullException(nameof(authorRepository));
        _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
    }

    public IReadOnlyList<Book> SearchBooks(BookFilter? filter, BookSort? sort)
    {
        filter ??= new BookFilter();
        IEnumerable<Book> query = _bookRepository.FindAll();

        if (!string.IsNullOrWhiteSpace(filter.TitleFragment))
        {
            var needle = filter.TitleFragment.ToLowerInvariant();
            query = query.Where(b => b.Title.ToLowerInvariant().Contains(needle));
        }
        if (filter.AuthorId is not null)
        {
            var authorId = filter.AuthorId;
            query = query.Where(b => b.Authors.Any(a => a.Id == authorId));
        }
        if (filter.CategoryId is not null)
        {
            var categoryId = filter.CategoryId;
            query = query.Where(b => b.Category is not null && b.Category.Id == categoryId);
        }
        if (filter.Language is not null)
        {
            var language = filter.Language;
            query = query.Where(b => string.Equals(b.Language, language, StringComparison.OrdinalIgnoreCase));
        }
        if (filter.FromYear is int from)
            query = query.Where(b => b.PublicationYear >= from);
        if (filter.ToYear is int to)
            query = query.Where(b => b.PublicationYear <= to);
        if (filter.OnlyAvailable == true)
            query = query.Where(b => b.IsAvailable);
        if (filter.MinRating is double min)
            query = query.Where(b => b.AverageRating >= min);

        return ApplySort(query, sort).ToList();
    }

    private static IEnumerable<Book> ApplySort(IEnumerable<Book> source, BookSort? sort)
    {
        return sort switch
        {
            BookSort.TitleAsc => source.OrderBy(b => b.Title, StringComparer.OrdinalIgnoreCase),
            BookSort.TitleDesc => source.OrderByDescending(b => b.Title, StringComparer.OrdinalIgnoreCase),
            BookSort.YearAsc => source.OrderBy(b => b.PublicationYear),
            BookSort.YearDesc => source.OrderByDescending(b => b.PublicationYear),
            BookSort.RatingDesc => source.OrderByDescending(b => b.AverageRating),
            BookSort.AvailabilityDesc => source.OrderByDescending(b => b.AvailableCopies),
            _ => source
        };
    }

    public IReadOnlyList<Author> SearchAuthors(string? fragment) =>
        _authorRepository.FindByNameContains(fragment);

    public IReadOnlyList<Member> SearchMembers(string? fragment) =>
        _memberRepository.FindByNameContains(fragment);

    public IReadOnlyList<Book> RecommendForMember(Member? member, int limit)
    {
        if (member is null || limit <= 0)
            return Array.Empty<Book>();

        var history = member.LoanHistoryIds
            .Select(id => _bookRepository.FindById(id))
            .Where(b => b is not null)
            .Cast<Book>()
            .ToList();

        if (history.Count == 0)
            return _bookRepository.FindTopRated(limit);

        var preferredCategoryIds = history
            .Where(b => b.Category is not null)
            .Select(b => b.Category!.Id)
            .Distinct()
            .ToList();

        return _bookRepository.FindAll()
            .Where(b => b.IsAvailable)
            .Where(b => !member.LoanHistoryIds.Contains(b.Id))
            .Where(b => b.Category is not null && preferredCategoryIds.Contains(b.Category.Id))
            .OrderByDescending(b => b.AverageRating)
            .Take(limit)
            .ToList();
    }

    public IReadOnlyList<Book> QuickSearch(string? term) =>
        SearchBooks(new BookFilter { TitleFragment = term }, BookSort.TitleAsc);

    public IReadOnlyList<Book> FindBooksByLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return Array.Empty<Book>();
        return _bookRepository.FindAll()
            .Where(b => string.Equals(b.Language, language, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public IReadOnlyList<Book> FindBooksPublishedAfter(int year) =>
        _bookRepository.FindAll()
            .Where(b => b.PublicationYear > year)
            .OrderByDescending(b => b.PublicationYear)
            .ToList();
}
