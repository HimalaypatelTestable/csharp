using System;
using System.Linq;
using Library.Model;
using Library.Repository;
using Library.Service;
using Library.Util;

namespace Library.App;

public class Program
{
    private readonly BookRepository _bookRepository;
    private readonly MemberRepository _memberRepository;
    private readonly LoanRepository _loanRepository;
    private readonly AuthorRepository _authorRepository;
    private readonly BookService _bookService;
    private readonly MemberService _memberService;
    private readonly LoanService _loanService;
    private readonly SearchService _searchService;
    private readonly ReportService _reportService;
    private readonly IdGenerator _idGenerator;

    public Program()
    {
        _bookRepository = new BookRepository();
        _memberRepository = new MemberRepository();
        _loanRepository = new LoanRepository();
        _authorRepository = new AuthorRepository();
        _idGenerator = new IdGenerator();
        _bookService = new BookService(_bookRepository, _authorRepository, _idGenerator);
        _memberService = new MemberService(_memberRepository, _idGenerator);
        _loanService = new LoanService(_loanRepository, _bookRepository, _memberRepository, _idGenerator);
        _searchService = new SearchService(_bookRepository, _authorRepository, _memberRepository);
        _reportService = new ReportService(
            _bookRepository, _memberRepository, _loanRepository, _authorRepository);
    }

    public static void Main(string[] args)
    {
        var app = new Program();
        app.SeedData();
        app.RunDemo();
    }

    public void SeedData()
    {
        Console.WriteLine("=== Seeding library data ===");

        var orwellHouse = new Publisher(_idGenerator.NextPublisherId(), "Orwell House")
        {
            Country = "UK",
            City = "London"
        };
        orwellHouse.SetFoundedYear(1935);
        orwellHouse.AddSpecialty("Literary Fiction");

        var techBooks = new Publisher(_idGenerator.NextPublisherId(), "TechBooks Press")
        {
            Country = "USA",
            City = "San Francisco"
        };
        techBooks.SetFoundedYear(1998);
        techBooks.AddSpecialty("Computer Science");

        var fiction = new Category(_idGenerator.NextCategoryId(), "Fiction");
        var scifi = new Category(_idGenerator.NextCategoryId(), "Science Fiction")
        {
            Parent = fiction
        };
        var tech = new Category(_idGenerator.NextCategoryId(), "Technology");
        var programming = new Category(_idGenerator.NextCategoryId(), "Programming")
        {
            Parent = tech
        };

        var orwell = new Author(_idGenerator.NextAuthorId(), "George", "Orwell")
        {
            Nationality = "British",
            Verified = true
        };
        orwell.SetBirthDate(new DateOnly(1903, 6, 25));
        orwell.SetDeathDate(new DateOnly(1950, 1, 21));
        orwell.AddGenre("Dystopian");
        orwell.AddGenre("Political Fiction");
        _authorRepository.Save(orwell);

        var knuth = new Author(_idGenerator.NextAuthorId(), "Donald", "Knuth")
        {
            Nationality = "American",
            Verified = true
        };
        knuth.SetBirthDate(new DateOnly(1938, 1, 10));
        knuth.AddGenre("Computer Science");
        knuth.AddAward("Turing Award");
        _authorRepository.Save(knuth);

        var nineteenEightyFour = _bookService.CreateBook("9780451524935", "1984", 1949, 3);
        _bookService.AssignAuthor(nineteenEightyFour.Id, orwell.Id);
        _bookService.AssignCategory(nineteenEightyFour.Id, scifi);
        _bookService.AssignPublisher(nineteenEightyFour.Id, orwellHouse);
        nineteenEightyFour.Price = 15.99;
        nineteenEightyFour.PageCount = 328;
        _bookRepository.Save(nineteenEightyFour);

        var animalFarm = _bookService.CreateBook("9780451526342", "Animal Farm", 1945, 2);
        _bookService.AssignAuthor(animalFarm.Id, orwell.Id);
        _bookService.AssignCategory(animalFarm.Id, fiction);
        _bookService.AssignPublisher(animalFarm.Id, orwellHouse);
        animalFarm.Price = 12.50;
        _bookRepository.Save(animalFarm);

        var taocp = _bookService.CreateBook("9780201896831", "The Art of Computer Programming", 1968, 1);
        _bookService.AssignAuthor(taocp.Id, knuth.Id);
        _bookService.AssignCategory(taocp.Id, programming);
        _bookService.AssignPublisher(taocp.Id, techBooks);
        taocp.Price = 89.99;
        _bookRepository.Save(taocp);

        var alice = _memberService.RegisterMember("Alice", "Walker", "alice@example.com");
        _memberService.UpdateContactInfo(alice.Id, "+1-555-0100", "123 Main St");

        var bob = _memberService.RegisterMember("Bob", "Martin", "bob@example.com");
        _memberService.UpgradeMembership(bob.Id, Member.MembershipType.Premium);

        var carol = _memberService.RegisterMember("Carol", "Davis", "carol@example.com");
        _memberService.UpgradeMembership(carol.Id, Member.MembershipType.Student);

        Console.WriteLine(
            $"Seeded {_bookRepository.Count} books, {_memberRepository.Count} members, "
            + $"{_authorRepository.Count} authors.");
    }

    public void RunDemo()
    {
        Console.WriteLine("\n=== Running demo workflows ===");

        var alice = _memberService.FindByEmail("alice@example.com")
            ?? throw new InvalidOperationException("Alice not found");
        var bob = _memberService.FindByEmail("bob@example.com")
            ?? throw new InvalidOperationException("Bob not found");

        var available = _bookService.FindAvailable();
        if (available.Count == 0)
        {
            Console.WriteLine("No books available to borrow.");
            return;
        }

        var first = available[0];
        var loan1 = _loanService.BorrowBook(alice.Id, first.Id);
        Console.WriteLine(
            $"Alice borrowed: {first.Title} (due {DateUtils.FormatDisplay(loan1.DueDate)})");

        if (available.Count > 1)
        {
            var second = available[1];
            var loan2 = _loanService.BorrowBook(bob.Id, second.Id);
            Console.WriteLine($"Bob borrowed: {second.Title}");
            _loanService.RenewLoan(loan2.Id);
            Console.WriteLine(
                $"Bob renewed loan — new due date: {DateUtils.FormatDisplay(loan2.DueDate)}");
        }

        _bookService.RateBook(first.Id, 4.5);
        _bookService.RateBook(first.Id, 5.0);

        var returned = _loanService.ReturnBook(loan1.Id);
        Console.WriteLine($"Alice returned book. Fine: {returned.FineAmount}");

        Console.WriteLine("\n=== Search ===");
        var orwellBooks = _searchService.SearchBooks(
            new SearchService.BookFilter { TitleFragment = "animal" },
            SearchService.BookSort.TitleAsc);
        foreach (var b in orwellBooks)
            Console.WriteLine($"  - {b.Title}");

        Console.WriteLine("\n=== Reports ===");
        foreach (var kv in _reportService.InventorySummary())
            Console.WriteLine($"  {kv.Key}: {kv.Value}");

        Console.WriteLine("\nMost borrowed books:");
        foreach (var b in _reportService.MostBorrowedBooks(5))
            Console.WriteLine($"  - {b.Title}");

        Console.WriteLine("\nTop authors by loans:");
        foreach (var a in _reportService.TopAuthorsByLoans(5))
            Console.WriteLine($"  - {a.DisplayName}");

        Console.WriteLine($"\nAverage loan duration: {_reportService.AverageLoanDuration()} days");
        Console.WriteLine($"Overdue rate: {_reportService.OverdueRate() * 100}%");
    }
}
