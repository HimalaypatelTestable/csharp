using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Library.Model;

namespace Library.Repository;

public class LoanRepository
{
    private readonly ConcurrentDictionary<string, Loan> _loansById = new();
    private readonly Dictionary<string, List<string>> _loansByMember = new();
    private readonly Dictionary<string, List<string>> _loansByBook = new();
    private readonly object _indexLock = new();

    public Loan Save(Loan loan)
    {
        ArgumentNullException.ThrowIfNull(loan);

        _loansById[loan.Id] = loan;
        lock (_indexLock)
        {
            if (!_loansByMember.TryGetValue(loan.MemberId, out var memberList))
            {
                memberList = new List<string>();
                _loansByMember[loan.MemberId] = memberList;
            }
            if (!memberList.Contains(loan.Id))
                memberList.Add(loan.Id);

            if (!_loansByBook.TryGetValue(loan.BookId, out var bookList))
            {
                bookList = new List<string>();
                _loansByBook[loan.BookId] = bookList;
            }
            if (!bookList.Contains(loan.Id))
                bookList.Add(loan.Id);
        }
        return loan;
    }

    public Loan? FindById(string? id)
    {
        if (id is null) return null;
        return _loansById.TryGetValue(id, out var l) ? l : null;
    }

    public IReadOnlyList<Loan> FindAll() => _loansById.Values.ToList();

    public IReadOnlyList<Loan> FindByMemberId(string? memberId)
    {
        if (memberId is null) return Array.Empty<Loan>();
        lock (_indexLock)
        {
            if (!_loansByMember.TryGetValue(memberId, out var ids))
                return Array.Empty<Loan>();
            return ids.Select(FindById).Where(l => l is not null).Cast<Loan>().ToList();
        }
    }

    public IReadOnlyList<Loan> FindActiveByMemberId(string? memberId) =>
        FindByMemberId(memberId).Where(l => l.IsActive).ToList();

    public IReadOnlyList<Loan> FindByBookId(string? bookId)
    {
        if (bookId is null) return Array.Empty<Loan>();
        lock (_indexLock)
        {
            if (!_loansByBook.TryGetValue(bookId, out var ids))
                return Array.Empty<Loan>();
            return ids.Select(FindById).Where(l => l is not null).Cast<Loan>().ToList();
        }
    }

    public IReadOnlyList<Loan> FindActive() =>
        _loansById.Values.Where(l => l.IsActive).ToList();

    public IReadOnlyList<Loan> FindOverdue()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return _loansById.Values
            .Where(l => l.IsOverdueOn(today))
            .OrderBy(l => l.DueDate)
            .ToList();
    }

    public IReadOnlyList<Loan> FindDueWithinDays(int days)
    {
        if (days < 0)
            throw new ArgumentException("Days cannot be negative", nameof(days));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(days);
        return _loansById.Values
            .Where(l => l.IsActive && l.DueDate >= today && l.DueDate <= cutoff)
            .OrderBy(l => l.DueDate)
            .ToList();
    }

    public IReadOnlyList<Loan> FindByStatus(Loan.LoanStatus status) =>
        _loansById.Values.Where(l => l.Status == status).ToList();

    public IReadOnlyList<Loan> FindBorrowedBetween(DateOnly from, DateOnly to)
    {
        if (from > to)
            throw new ArgumentException("Invalid date range");
        return _loansById.Values
            .Where(l => l.BorrowDate >= from && l.BorrowDate <= to)
            .ToList();
    }

    public IReadOnlyList<Loan> FindReturnedBetween(DateOnly from, DateOnly to)
    {
        if (from > to)
            throw new ArgumentException("Invalid date range");
        return _loansById.Values
            .Where(l => l.ReturnDate is not null
                && l.ReturnDate.Value >= from
                && l.ReturnDate.Value <= to)
            .ToList();
    }

    public bool DeleteById(string id)
    {
        if (!_loansById.TryRemove(id, out var removed))
            return false;
        lock (_indexLock)
        {
            if (_loansByMember.TryGetValue(removed.MemberId, out var memberList))
                memberList.Remove(id);
            if (_loansByBook.TryGetValue(removed.BookId, out var bookList))
                bookList.Remove(id);
        }
        return true;
    }

    public int Count => _loansById.Count;

    public int CountActive() => _loansById.Values.Count(l => l.IsActive);

    public int CountOverdue() => FindOverdue().Count;

    public double TotalFinesAccrued() =>
        _loansById.Values.Sum(l => l.FineAmount);

    public bool ExistsById(string? id) =>
        id is not null && _loansById.ContainsKey(id);

    public void Clear()
    {
        _loansById.Clear();
        lock (_indexLock)
        {
            _loansByMember.Clear();
            _loansByBook.Clear();
        }
    }
}
