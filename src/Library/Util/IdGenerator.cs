using System;
using System.Threading;

namespace Library.Util;

public class IdGenerator
{
    private const string BookPrefix = "BK";
    private const string MemberPrefix = "MB";
    private const string LoanPrefix = "LN";
    private const string AuthorPrefix = "AU";
    private const string PublisherPrefix = "PB";
    private const string CategoryPrefix = "CT";
    private const string ReservationPrefix = "RS";
    private const string FinePrefix = "FN";

    private long _bookSequence;
    private long _memberSequence;
    private long _loanSequence;
    private long _authorSequence;
    private long _publisherSequence;
    private long _categorySequence;
    private long _reservationSequence;
    private long _fineSequence;
    private readonly int _padWidth;

    public IdGenerator() : this(6) { }

    public IdGenerator(int padWidth)
    {
        if (padWidth < 1 || padWidth > 12)
            throw new ArgumentException("Pad width must be between 1 and 12", nameof(padWidth));
        _padWidth = padWidth;
    }

    public string NextBookId() =>
        Format(BookPrefix, Interlocked.Increment(ref _bookSequence));

    public string NextMemberId() =>
        Format(MemberPrefix, Interlocked.Increment(ref _memberSequence));

    public string NextLoanId() =>
        Format(LoanPrefix, Interlocked.Increment(ref _loanSequence));

    public string NextAuthorId() =>
        Format(AuthorPrefix, Interlocked.Increment(ref _authorSequence));

    public string NextPublisherId() =>
        Format(PublisherPrefix, Interlocked.Increment(ref _publisherSequence));

    public string NextCategoryId() =>
        Format(CategoryPrefix, Interlocked.Increment(ref _categorySequence));

    public string NextReservationId() =>
        Format(ReservationPrefix, Interlocked.Increment(ref _reservationSequence));

    public string NextFineId() =>
        Format(FinePrefix, Interlocked.Increment(ref _fineSequence));

    private string Format(string prefix, long value) => $"{prefix}-{Pad(value)}";

    private string Pad(long value)
    {
        var raw = value.ToString();
        return raw.Length >= _padWidth ? raw : raw.PadLeft(_padWidth, '0');
    }

    public string RandomGuid() => Guid.NewGuid().ToString();

    public string DateStampedId(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Prefix cannot be null or blank", nameof(prefix));
        var dateStamp = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyyMMdd");
        return $"{prefix.ToUpperInvariant()}-{dateStamp}-{Pad(NextSequenceFor(prefix))}";
    }

    private long NextSequenceFor(string prefix)
    {
        return prefix.ToUpperInvariant() switch
        {
            "BK" => Interlocked.Increment(ref _bookSequence),
            "MB" => Interlocked.Increment(ref _memberSequence),
            "LN" => Interlocked.Increment(ref _loanSequence),
            "AU" => Interlocked.Increment(ref _authorSequence),
            "PB" => Interlocked.Increment(ref _publisherSequence),
            "CT" => Interlocked.Increment(ref _categorySequence),
            "RS" => Interlocked.Increment(ref _reservationSequence),
            "FN" => Interlocked.Increment(ref _fineSequence),
            _ => DateTime.UtcNow.Ticks
        };
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _bookSequence, 0);
        Interlocked.Exchange(ref _memberSequence, 0);
        Interlocked.Exchange(ref _loanSequence, 0);
        Interlocked.Exchange(ref _authorSequence, 0);
        Interlocked.Exchange(ref _publisherSequence, 0);
        Interlocked.Exchange(ref _categorySequence, 0);
        Interlocked.Exchange(ref _reservationSequence, 0);
        Interlocked.Exchange(ref _fineSequence, 0);
    }

    public long BookCount => Interlocked.Read(ref _bookSequence);

    public long MemberCount => Interlocked.Read(ref _memberSequence);

    public long LoanCount => Interlocked.Read(ref _loanSequence);

    public long AuthorCount => Interlocked.Read(ref _authorSequence);

    public long PublisherCount => Interlocked.Read(ref _publisherSequence);

    public long CategoryCount => Interlocked.Read(ref _categorySequence);

    public long ReservationCount => Interlocked.Read(ref _reservationSequence);

    public long FineCount => Interlocked.Read(ref _fineSequence);

    public long TotalIdsIssued =>
        BookCount + MemberCount + LoanCount + AuthorCount
        + PublisherCount + CategoryCount + ReservationCount + FineCount;

    public static bool IsValidId(string? id, string? expectedPrefix)
    {
        if (id is null || expectedPrefix is null) return false;
        var expected = expectedPrefix + "-";
        if (!id.StartsWith(expected, StringComparison.Ordinal)) return false;
        var suffix = id[expected.Length..];
        if (suffix.Length == 0) return false;
        foreach (var c in suffix)
            if (!char.IsDigit(c)) return false;
        return true;
    }

    public static string? ExtractPrefix(string? id)
    {
        if (id is null) return null;
        var dash = id.IndexOf('-');
        return dash > 0 ? id[..dash] : null;
    }

    public static long ExtractSequence(string? id)
    {
        if (id is null) return -1;
        var dash = id.IndexOf('-');
        if (dash < 0) return -1;
        return long.TryParse(id[(dash + 1)..], out var value) ? value : -1;
    }
}
