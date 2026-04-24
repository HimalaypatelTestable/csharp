using System;
using System.Collections.Generic;
using Library.Exceptions;
using Library.Model;
using Library.Repository;
using Library.Util;

namespace Library.Service;

public class LoanService
{
    private readonly LoanRepository _loanRepository;
    private readonly BookRepository _bookRepository;
    private readonly MemberRepository _memberRepository;
    private readonly IdGenerator _idGenerator;
    private readonly object _mutex = new();

    public LoanService(LoanRepository loanRepository,
                       BookRepository bookRepository,
                       MemberRepository memberRepository,
                       IdGenerator idGenerator)
    {
        _loanRepository = loanRepository ?? throw new ArgumentNullException(nameof(loanRepository));
        _bookRepository = bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));
        _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
    }

    public Loan BorrowBook(string memberId, string bookId)
    {
        lock (_mutex)
        {
            var member = RequireMember(memberId);
            var book = RequireBook(bookId);
            if (!member.CanBorrow())
                throw new LibraryException($"Member is not eligible to borrow: {memberId}");
            if (!book.IsAvailable)
                throw new LibraryException($"Book is not available: {bookId}");
            if (!book.Borrow())
                throw new LibraryException($"Failed to reserve a copy of book: {bookId}");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dueDate = today.AddDays(member.LoanDurationDays);
            var loanId = _idGenerator.NextLoanId();
            var loan = new Loan(loanId, bookId, memberId, today, dueDate);
            try
            {
                member.AddActiveLoan(loanId);
            }
            catch (InvalidOperationException e)
            {
                book.ReturnCopy();
                throw new LibraryException($"Cannot assign loan to member: {e.Message}");
            }

            _loanRepository.Save(loan);
            _bookRepository.Save(book);
            _memberRepository.Save(member);
            return loan;
        }
    }

    public Loan ReturnBook(string loanId) =>
        ReturnBookOn(loanId, DateOnly.FromDateTime(DateTime.UtcNow));

    public Loan ReturnBookOn(string loanId, DateOnly returnDate)
    {
        lock (_mutex)
        {
            var loan = RequireLoan(loanId);
            if (!loan.IsActive)
                throw new LibraryException($"Loan is not active: {loanId}");
            var book = RequireBook(loan.BookId);
            var member = RequireMember(loan.MemberId);

            var fine = loan.ReturnOn(returnDate, book.Price);
            if (fine > 0) member.AddFine(fine);
            book.ReturnCopy();
            member.CompleteLoan(loanId);

            _loanRepository.Save(loan);
            _bookRepository.Save(book);
            _memberRepository.Save(member);
            return loan;
        }
    }

    public Loan RenewLoan(string loanId)
    {
        lock (_mutex)
        {
            var loan = RequireLoan(loanId);
            var member = RequireMember(loan.MemberId);
            if (!loan.CanRenew)
                throw new LibraryException($"Loan cannot be renewed: {loanId}");
            if (!loan.Renew(member.LoanDurationDays))
                throw new LibraryException($"Renewal refused: {loanId}");
            _loanRepository.Save(loan);
            return loan;
        }
    }

    public Loan ReportLost(string loanId)
    {
        lock (_mutex)
        {
            var loan = RequireLoan(loanId);
            if (!loan.IsActive)
                throw new LibraryException($"Loan is not active: {loanId}");
            var book = RequireBook(loan.BookId);
            var member = RequireMember(loan.MemberId);

            var fine = loan.MarkLost(book.Price);
            member.AddFine(fine);
            member.CompleteLoan(loanId);
            book.Status = Book.BookStatus.Lost;

            _loanRepository.Save(loan);
            _bookRepository.Save(book);
            _memberRepository.Save(member);
            return loan;
        }
    }

    public void RefreshOverdueLoans()
    {
        foreach (var loan in _loanRepository.FindActive())
        {
            var before = loan.Status;
            loan.RefreshOverdueStatus();
            if (loan.Status != before)
                _loanRepository.Save(loan);
        }
    }

    public IReadOnlyList<Loan> FindLoansForMember(string memberId)
    {
        RequireMember(memberId);
        return _loanRepository.FindByMemberId(memberId);
    }

    public IReadOnlyList<Loan> FindActiveLoansForMember(string memberId)
    {
        RequireMember(memberId);
        return _loanRepository.FindActiveByMemberId(memberId);
    }

    public IReadOnlyList<Loan> FindLoansForBook(string bookId)
    {
        RequireBook(bookId);
        return _loanRepository.FindByBookId(bookId);
    }

    public IReadOnlyList<Loan> FindOverdueLoans() => _loanRepository.FindOverdue();

    public IReadOnlyList<Loan> FindLoansDueSoon(int days) =>
        _loanRepository.FindDueWithinDays(days);

    private Loan RequireLoan(string loanId)
    {
        return _loanRepository.FindById(loanId)
            ?? throw new LibraryException($"Loan not found: {loanId}");
    }

    private Book RequireBook(string bookId)
    {
        return _bookRepository.FindById(bookId)
            ?? throw new LibraryException($"Book not found: {bookId}");
    }

    private Member RequireMember(string memberId)
    {
        return _memberRepository.FindById(memberId)
            ?? throw new LibraryException($"Member not found: {memberId}");
    }
}
