using System;

namespace Library.Model;

public class Loan
{
    public enum LoanStatus
    {
        Active,
        Returned,
        Overdue,
        Lost,
        Renewed
    }

    private const double DailyLateFee = 0.50;
    private const double LostBookMultiplier = 1.25;
    private const int MaxRenewals = 2;

    public Loan(string id, string bookId, string memberId, DateOnly borrowDate, DateOnly dueDate)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Loan id cannot be null or blank", nameof(id));
        if (string.IsNullOrWhiteSpace(bookId))
            throw new ArgumentException("Book id cannot be null or blank", nameof(bookId));
        if (string.IsNullOrWhiteSpace(memberId))
            throw new ArgumentException("Member id cannot be null or blank", nameof(memberId));
        if (dueDate < borrowDate)
            throw new ArgumentException("Due date cannot be before borrow date");

        Id = id;
        BookId = bookId;
        MemberId = memberId;
        BorrowDate = borrowDate;
        DueDate = dueDate;
        Status = LoanStatus.Active;
        RenewalCount = 0;
        FineAmount = 0.0;
        LastStatusChange = borrowDate;
    }

    public string Id { get; }

    public string BookId { get; }

    public string MemberId { get; }

    public DateOnly BorrowDate { get; }

    public DateOnly DueDate { get; private set; }

    public DateOnly? ReturnDate { get; private set; }

    public LoanStatus Status { get; private set; }

    public int RenewalCount { get; private set; }

    public double FineAmount { get; private set; }

    public string? Notes { get; set; }

    public DateOnly LastStatusChange { get; private set; }

    public bool IsActive =>
        Status == LoanStatus.Active || Status == LoanStatus.Renewed || Status == LoanStatus.Overdue;

    public bool IsOverdue => IsOverdueOn(DateOnly.FromDateTime(DateTime.UtcNow));

    public bool IsOverdueOn(DateOnly date)
    {
        if (!IsActive)
            return false;
        return date > DueDate;
    }

    public long DaysOverdueOn(DateOnly date)
    {
        if (!IsOverdueOn(date))
            return 0;
        return date.DayNumber - DueDate.DayNumber;
    }

    public long DaysUntilDue =>
        DueDate.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;

    public bool CanRenew =>
        IsActive && !IsOverdue && RenewalCount < MaxRenewals;

    public bool Renew(int additionalDays)
    {
        if (!CanRenew) return false;
        if (additionalDays <= 0)
            throw new ArgumentException("Additional days must be positive", nameof(additionalDays));

        DueDate = DueDate.AddDays(additionalDays);
        RenewalCount += 1;
        Status = LoanStatus.Renewed;
        LastStatusChange = DateOnly.FromDateTime(DateTime.UtcNow);
        return true;
    }

    public double ReturnOn(DateOnly date, double bookPrice)
    {
        if (!IsActive)
            throw new InvalidOperationException($"Cannot return a loan with status {Status}");
        if (date < BorrowDate)
            throw new ArgumentException("Return date cannot be before borrow date", nameof(date));

        ReturnDate = date;
        var overdueDays = DaysOverdueOn(date);
        if (overdueDays > 0)
            FineAmount = overdueDays * DailyLateFee;
        Status = LoanStatus.Returned;
        LastStatusChange = date;
        return FineAmount;
    }

    public double MarkLost(double bookPrice)
    {
        if (bookPrice < 0)
            throw new ArgumentException("Book price cannot be negative", nameof(bookPrice));
        FineAmount = bookPrice * LostBookMultiplier;
        Status = LoanStatus.Lost;
        LastStatusChange = DateOnly.FromDateTime(DateTime.UtcNow);
        return FineAmount;
    }

    public void RefreshOverdueStatus()
    {
        if (Status == LoanStatus.Active || Status == LoanStatus.Renewed)
        {
            if (IsOverdue)
            {
                Status = LoanStatus.Overdue;
                LastStatusChange = DateOnly.FromDateTime(DateTime.UtcNow);
            }
        }
    }

    public long LoanDurationDays
    {
        get
        {
            var end = ReturnDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            return end.DayNumber - BorrowDate.DayNumber;
        }
    }

    public override bool Equals(object? obj) => obj is Loan l && l.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString()
        => $"Loan{{Id='{Id}', Book='{BookId}', Member='{MemberId}', Due={DueDate}, Status={Status}, Fine={FineAmount}}}";
}
