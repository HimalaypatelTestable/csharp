using System;
using System.Collections.Generic;
using System.Linq;
using Library.Model;
using Library.Repository;

namespace Library.Service;

public class ReportService
{
    private readonly BookRepository _bookRepository;
    private readonly MemberRepository _memberRepository;
    private readonly LoanRepository _loanRepository;
    private readonly AuthorRepository _authorRepository;

    public ReportService(BookRepository bookRepository,
                         MemberRepository memberRepository,
                         LoanRepository loanRepository,
                         AuthorRepository authorRepository)
    {
        _bookRepository = bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));
        _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
        _loanRepository = loanRepository ?? throw new ArgumentNullException(nameof(loanRepository));
        _authorRepository = authorRepository ?? throw new ArgumentNullException(nameof(authorRepository));
    }

    public IReadOnlyDictionary<string, object> InventorySummary()
    {
        var summary = new Dictionary<string, object>
        {
            { "totalBooks", _bookRepository.Count },
            { "availableBooks", _bookRepository.CountAvailable() },
            { "totalMembers", _memberRepository.Count },
            { "activeMembers", _memberRepository.CountActive() },
            { "totalLoans", _loanRepository.Count },
            { "activeLoans", _loanRepository.CountActive() },
            { "overdueLoans", _loanRepository.CountOverdue() },
            { "totalAuthors", _authorRepository.Count },
            { "outstandingFines", _memberRepository.TotalOutstandingFines() },
            { "generatedAt", DateOnly.FromDateTime(DateTime.UtcNow).ToString("O") }
        };
        return summary;
    }

    public IReadOnlyList<Book> MostBorrowedBooks(int limit)
    {
        if (limit <= 0) return Array.Empty<Book>();
        var counts = _loanRepository.FindAll()
            .GroupBy(l => l.BookId)
            .OrderByDescending(g => g.Count())
            .Take(limit);
        return counts
            .Select(g => _bookRepository.FindById(g.Key))
            .Where(b => b is not null)
            .Cast<Book>()
            .ToList();
    }

    public IReadOnlyList<Member> MostActiveMembers(int limit)
    {
        if (limit <= 0) return Array.Empty<Member>();
        var counts = _loanRepository.FindAll()
            .GroupBy(l => l.MemberId)
            .OrderByDescending(g => g.Count())
            .Take(limit);
        return counts
            .Select(g => _memberRepository.FindById(g.Key))
            .Where(m => m is not null)
            .Cast<Member>()
            .ToList();
    }

    public IReadOnlyDictionary<string, int> LoanCountByCategory()
    {
        var counts = new Dictionary<string, int>();
        foreach (var loan in _loanRepository.FindAll())
        {
            var book = _bookRepository.FindById(loan.BookId);
            if (book?.Category is null) continue;
            var key = book.Category.Name;
            counts[key] = counts.TryGetValue(key, out var existing) ? existing + 1 : 1;
        }
        return counts
            .OrderByDescending(kv => kv.Value)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public double AverageLoanDuration()
    {
        var returned = _loanRepository.FindAll()
            .Where(l => l.ReturnDate is not null)
            .ToList();
        if (returned.Count == 0) return 0.0;
        var totalDays = returned.Sum(l => (long)(l.ReturnDate!.Value.DayNumber - l.BorrowDate.DayNumber));
        return (double)totalDays / returned.Count;
    }

    public double OverdueRate()
    {
        var total = _loanRepository.Count;
        if (total == 0) return 0.0;
        var overdue = _loanRepository.FindAll()
            .Count(l => l.ReturnDate is not null && l.ReturnDate.Value > l.DueDate);
        return (double)overdue / total;
    }

    public IReadOnlyList<Author> TopAuthorsByLoans(int limit)
    {
        if (limit <= 0) return Array.Empty<Author>();
        var loansPerAuthor = new Dictionary<string, int>();
        foreach (var loan in _loanRepository.FindAll())
        {
            var book = _bookRepository.FindById(loan.BookId);
            if (book is null) continue;
            foreach (var author in book.Authors)
            {
                loansPerAuthor[author.Id] =
                    loansPerAuthor.TryGetValue(author.Id, out var existing) ? existing + 1 : 1;
            }
        }
        return loansPerAuthor
            .OrderByDescending(kv => kv.Value)
            .Take(limit)
            .Select(kv => _authorRepository.FindById(kv.Key))
            .Where(a => a is not null)
            .Cast<Author>()
            .ToList();
    }

    public IReadOnlyDictionary<string, double> FinesByMembershipType()
    {
        return _memberRepository.FindAll()
            .GroupBy(m => m.Type.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(m => m.OutstandingFine));
    }

    public IReadOnlyList<Book> NeverBorrowed()
    {
        var borrowedIds = _loanRepository.FindAll()
            .Select(l => l.BookId)
            .Distinct()
            .ToHashSet();
        return _bookRepository.FindAll()
            .Where(b => !borrowedIds.Contains(b.Id))
            .OrderBy(b => b.AddedDate)
            .ToList();
    }
}
