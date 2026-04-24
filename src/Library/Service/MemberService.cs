using System;
using System.Collections.Generic;
using Library.Exceptions;
using Library.Model;
using Library.Repository;
using Library.Util;

namespace Library.Service;

public class MemberService
{
    private const double FineCapForBorrowing = 50.0;

    private readonly MemberRepository _memberRepository;
    private readonly IdGenerator _idGenerator;

    public MemberService(MemberRepository memberRepository, IdGenerator idGenerator)
    {
        _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
    }

    public Member RegisterMember(string firstName, string lastName, string email)
    {
        ValidationUtils.RequireNonBlank(firstName, nameof(firstName));
        ValidationUtils.RequireNonBlank(lastName, nameof(lastName));
        ValidationUtils.RequireEmail(email);
        if (_memberRepository.ExistsByEmail(email))
            throw new LibraryException($"Email already registered: {email}");

        var id = _idGenerator.NextMemberId();
        var member = new Member(id, firstName, lastName, email);
        return _memberRepository.Save(member);
    }

    public Member UpdateContactInfo(string memberId, string? phone, string? address)
    {
        var member = RequireMember(memberId);
        if (phone is not null) member.Phone = phone;
        if (address is not null) member.Address = address;
        return _memberRepository.Save(member);
    }

    public Member ChangeEmail(string memberId, string newEmail)
    {
        var member = RequireMember(memberId);
        ValidationUtils.RequireEmail(newEmail);
        if (_memberRepository.ExistsByEmail(newEmail)
            && !string.Equals(member.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            throw new LibraryException($"Email already registered: {newEmail}");
        member.Email = newEmail;
        return _memberRepository.Save(member);
    }

    public Member UpgradeMembership(string memberId, Member.MembershipType newType)
    {
        var member = RequireMember(memberId);
        member.Type = newType;
        return _memberRepository.Save(member);
    }

    public Member RenewMembership(string memberId, int months)
    {
        var member = RequireMember(memberId);
        if (member.OutstandingFine > 0)
            throw new LibraryException("Cannot renew membership with outstanding fines");
        member.ExtendMembership(months);
        return _memberRepository.Save(member);
    }

    public Member SuspendMember(string memberId, string? reason = null)
    {
        var member = RequireMember(memberId);
        member.Status = Member.MemberStatus.Suspended;
        return _memberRepository.Save(member);
    }

    public Member ReactivateMember(string memberId)
    {
        var member = RequireMember(memberId);
        if (member.MembershipExpiry < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new LibraryException("Cannot reactivate expired membership without renewal");
        member.Status = Member.MemberStatus.Active;
        return _memberRepository.Save(member);
    }

    public Member CloseMembership(string memberId)
    {
        var member = RequireMember(memberId);
        if (member.ActiveLoanIds.Count > 0)
            throw new LibraryException("Cannot close membership with active loans");
        if (member.OutstandingFine > 0)
            throw new LibraryException("Cannot close membership with outstanding fines");
        member.Status = Member.MemberStatus.Closed;
        return _memberRepository.Save(member);
    }

    public Member PayFine(string memberId, double amount)
    {
        var member = RequireMember(memberId);
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be positive", nameof(amount));
        member.PayFine(amount);
        return _memberRepository.Save(member);
    }

    public bool IsEligibleToBorrow(string memberId)
    {
        var member = RequireMember(memberId);
        if (member.Status != Member.MemberStatus.Active) return false;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (member.MembershipExpiry < today) return false;
        if (member.OutstandingFine >= FineCapForBorrowing) return false;
        return member.ActiveLoanIds.Count < member.MaxActiveLoans;
    }

    public void ExpireMembershipsIfNeeded()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var member in _memberRepository.FindExpiringBefore(today))
        {
            if (member.Status == Member.MemberStatus.Active)
            {
                member.Status = Member.MemberStatus.Expired;
                _memberRepository.Save(member);
            }
        }
    }

    public Member? FindById(string memberId) => _memberRepository.FindById(memberId);

    public Member? FindByEmail(string email) => _memberRepository.FindByEmail(email);

    public IReadOnlyList<Member> FindAll() => _memberRepository.FindAll();

    public IReadOnlyList<Member> FindMembersWithFines() =>
        _memberRepository.FindWithOutstandingFines();

    public IReadOnlyList<Member> FindTopReaders(int limit) =>
        _memberRepository.FindTopReaders(limit);

    private Member RequireMember(string memberId)
    {
        return _memberRepository.FindById(memberId)
            ?? throw new LibraryException($"Member not found: {memberId}");
    }
}
