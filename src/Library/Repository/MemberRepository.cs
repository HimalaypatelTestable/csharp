using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Library.Model;

namespace Library.Repository;

public class MemberRepository
{
    private readonly ConcurrentDictionary<string, Member> _membersById = new();
    private readonly ConcurrentDictionary<string, string> _emailIndex = new();
    private readonly object _lock = new();

    public Member Save(Member member)
    {
        ArgumentNullException.ThrowIfNull(member);

        lock (_lock)
        {
            if (_membersById.TryGetValue(member.Id, out var existing)
                && !string.Equals(existing.Email, member.Email, StringComparison.OrdinalIgnoreCase))
            {
                _emailIndex.TryRemove(existing.Email.ToLowerInvariant(), out _);
            }

            var emailKey = member.Email.ToLowerInvariant();
            if (_emailIndex.TryGetValue(emailKey, out var holder) && holder != member.Id)
                throw new InvalidOperationException(
                    $"Email already registered to another member: {member.Email}");

            _membersById[member.Id] = member;
            _emailIndex[emailKey] = member.Id;
            return member;
        }
    }

    public Member? FindById(string? id)
    {
        if (id is null) return null;
        return _membersById.TryGetValue(id, out var m) ? m : null;
    }

    public Member? FindByEmail(string? email)
    {
        if (email is null) return null;
        if (_emailIndex.TryGetValue(email.ToLowerInvariant(), out var id))
            return FindById(id);
        return null;
    }

    public IReadOnlyList<Member> FindAll() => _membersById.Values.ToList();

    public IReadOnlyList<Member> FindByStatus(Member.MemberStatus status) =>
        _membersById.Values.Where(m => m.Status == status).ToList();

    public IReadOnlyList<Member> FindByMembershipType(Member.MembershipType type) =>
        _membersById.Values.Where(m => m.Type == type).ToList();

    public IReadOnlyList<Member> FindWithOutstandingFines() =>
        _membersById.Values
            .Where(m => m.OutstandingFine > 0)
            .OrderByDescending(m => m.OutstandingFine)
            .ToList();

    public IReadOnlyList<Member> FindExpiringBefore(DateOnly date) =>
        _membersById.Values
            .Where(m => m.MembershipExpiry < date)
            .ToList();

    public IReadOnlyList<Member> FindExpired()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return _membersById.Values
            .Where(m => m.MembershipExpiry < today)
            .ToList();
    }

    public IReadOnlyList<Member> FindTopReaders(int limit)
    {
        if (limit <= 0) return Array.Empty<Member>();
        return _membersById.Values
            .OrderByDescending(m => m.TotalBooksRead)
            .Take(limit)
            .ToList();
    }

    public IReadOnlyList<Member> FindByNameContains(string? fragment)
    {
        if (string.IsNullOrWhiteSpace(fragment))
            return Array.Empty<Member>();
        var needle = fragment.ToLowerInvariant();
        return _membersById.Values
            .Where(m => m.FullName.ToLowerInvariant().Contains(needle))
            .ToList();
    }

    public bool DeleteById(string id)
    {
        lock (_lock)
        {
            if (!_membersById.TryRemove(id, out var removed))
                return false;
            _emailIndex.TryRemove(removed.Email.ToLowerInvariant(), out _);
            return true;
        }
    }

    public int Count => _membersById.Count;

    public int CountActive() =>
        _membersById.Values.Count(m => m.Status == Member.MemberStatus.Active);

    public bool ExistsById(string? id) =>
        id is not null && _membersById.ContainsKey(id);

    public bool ExistsByEmail(string? email) =>
        email is not null && _emailIndex.ContainsKey(email.ToLowerInvariant());

    public double TotalOutstandingFines() =>
        _membersById.Values.Sum(m => m.OutstandingFine);

    public IReadOnlyDictionary<Member.MembershipType, int> CountByType()
    {
        return _membersById.Values
            .GroupBy(m => m.Type)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public void Clear()
    {
        lock (_lock)
        {
            _membersById.Clear();
            _emailIndex.Clear();
        }
    }
}
