using System;
using System.Collections.Generic;

namespace Library.Model;

public class Member
{
    public enum MembershipType
    {
        Standard,
        Student,
        Premium,
        Staff,
        Child
    }

    public enum MemberStatus
    {
        Active,
        Suspended,
        Expired,
        Closed
    }

    private static readonly Dictionary<MembershipType, (int MaxLoans, int LoanDays)> TypeLimits = new()
    {
        { MembershipType.Standard, (3, 14) },
        { MembershipType.Student, (5, 21) },
        { MembershipType.Premium, (10, 30) },
        { MembershipType.Staff, (15, 60) },
        { MembershipType.Child, (2, 14) }
    };

    private readonly List<string> _activeLoanIds = new();
    private readonly List<string> _loanHistoryIds = new();
    private string _firstName;
    private string _lastName;
    private string _email;

    public Member(string id, string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Member id cannot be null or blank", nameof(id));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be null or blank", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be null or blank", nameof(lastName));
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("Invalid email", nameof(email));

        Id = id;
        _firstName = firstName;
        _lastName = lastName;
        _email = email;
        JoinDate = DateOnly.FromDateTime(DateTime.UtcNow);
        MembershipExpiry = JoinDate.AddYears(1);
        Type = MembershipType.Standard;
        Status = MemberStatus.Active;
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

    public string Email
    {
        get => _email;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
                throw new ArgumentException("Invalid email", nameof(value));
            _email = value;
        }
    }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public int Age
    {
        get
        {
            if (DateOfBirth is null) return 0;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var age = today.Year - DateOfBirth.Value.Year;
            if (DateOfBirth.Value > today.AddYears(-age)) age -= 1;
            return age;
        }
    }

    public DateOnly JoinDate { get; }

    public DateOnly MembershipExpiry { get; private set; }

    public MembershipType Type { get; set; }

    public MemberStatus Status { get; set; }

    public double OutstandingFine { get; private set; }

    public IReadOnlyList<string> ActiveLoanIds => _activeLoanIds.AsReadOnly();

    public IReadOnlyList<string> LoanHistoryIds => _loanHistoryIds.AsReadOnly();

    public int TotalBooksRead { get; private set; }

    public int MaxActiveLoans => TypeLimits[Type].MaxLoans;

    public int LoanDurationDays => TypeLimits[Type].LoanDays;

    public void ExtendMembership(int months)
    {
        if (months <= 0)
            throw new ArgumentException("Extension months must be positive", nameof(months));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var baseDate = MembershipExpiry < today ? today : MembershipExpiry;
        MembershipExpiry = baseDate.AddMonths(months);
        if (Status == MemberStatus.Expired)
            Status = MemberStatus.Active;
    }

    public void AddFine(double amount)
    {
        if (amount < 0)
            throw new ArgumentException("Fine amount cannot be negative", nameof(amount));
        OutstandingFine += amount;
    }

    public void PayFine(double amount)
    {
        if (amount < 0)
            throw new ArgumentException("Payment amount cannot be negative", nameof(amount));
        if (amount > OutstandingFine)
            throw new ArgumentException("Payment exceeds outstanding fine", nameof(amount));
        OutstandingFine -= amount;
    }

    public void AddActiveLoan(string loanId)
    {
        if (_activeLoanIds.Count >= MaxActiveLoans)
            throw new InvalidOperationException($"Active loan limit reached for {Type}");
        _activeLoanIds.Add(loanId);
    }

    public void CompleteLoan(string loanId)
    {
        if (_activeLoanIds.Remove(loanId))
        {
            _loanHistoryIds.Add(loanId);
            TotalBooksRead += 1;
        }
    }

    public bool CanBorrow()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return Status == MemberStatus.Active
               && MembershipExpiry > today
               && _activeLoanIds.Count < MaxActiveLoans
               && OutstandingFine < 50.0;
    }

    public override bool Equals(object? obj) => obj is Member m && m.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString()
        => $"Member{{Id='{Id}', Name='{FullName}', Type={Type}, Status={Status}, ActiveLoans={_activeLoanIds.Count}}}";
}
