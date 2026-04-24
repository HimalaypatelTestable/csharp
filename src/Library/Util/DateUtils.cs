using System;
using System.Collections.Generic;
using System.Globalization;

namespace Library.Util;

public static class DateUtils
{
    public const string IsoFormat = "yyyy-MM-dd";
    public const string DisplayFormat = "dd MMM yyyy";
    public const string CompactFormat = "yyyyMMdd";

    private static readonly HashSet<DayOfWeek> Weekend = new()
    {
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    };

    public static string FormatIso(DateOnly? date) =>
        date?.ToString(IsoFormat, CultureInfo.InvariantCulture) ?? string.Empty;

    public static string FormatDisplay(DateOnly? date) =>
        date?.ToString(DisplayFormat, CultureInfo.InvariantCulture) ?? string.Empty;

    public static string FormatCompact(DateOnly? date) =>
        date?.ToString(CompactFormat, CultureInfo.InvariantCulture) ?? string.Empty;

    public static DateOnly? ParseIso(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        if (DateOnly.TryParseExact(text, IsoFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var value))
            return value;
        throw new ArgumentException($"Invalid ISO date: {text}", nameof(text));
    }

    public static DateOnly? SafeParseIso(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        return DateOnly.TryParseExact(text, IsoFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var value) ? value : null;
    }

    public static long DaysBetween(DateOnly start, DateOnly end) =>
        end.DayNumber - start.DayNumber;

    public static int MonthsBetween(DateOnly start, DateOnly end)
    {
        var months = (end.Year - start.Year) * 12 + (end.Month - start.Month);
        if (end.Day < start.Day) months -= 1;
        return months;
    }

    public static int YearsBetween(DateOnly start, DateOnly end)
    {
        var years = end.Year - start.Year;
        if (end.Month < start.Month || (end.Month == start.Month && end.Day < start.Day))
            years -= 1;
        return years;
    }

    public static bool IsWeekend(DateOnly date) => Weekend.Contains(date.DayOfWeek);

    public static bool IsWeekday(DateOnly date) => !IsWeekend(date);

    public static DateOnly AddBusinessDays(DateOnly from, int days)
    {
        var result = from;
        var direction = days >= 0 ? 1 : -1;
        var remaining = Math.Abs(days);
        while (remaining > 0)
        {
            result = result.AddDays(direction);
            if (IsWeekday(result))
                remaining -= 1;
        }
        return result;
    }

    public static long BusinessDaysBetween(DateOnly start, DateOnly end)
    {
        if (end < start)
            return -BusinessDaysBetween(end, start);
        long count = 0;
        var cursor = start;
        while (cursor < end)
        {
            if (IsWeekday(cursor))
                count += 1;
            cursor = cursor.AddDays(1);
        }
        return count;
    }

    public static DateOnly StartOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    public static DateOnly EndOfMonth(DateOnly date) =>
        new(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));

    public static DateOnly StartOfYear(DateOnly date) => new(date.Year, 1, 1);

    public static DateOnly EndOfYear(DateOnly date) => new(date.Year, 12, 31);

    public static bool IsBetween(DateOnly date, DateOnly start, DateOnly end) =>
        date >= start && date <= end;

    public static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;

    public static DateOnly Max(DateOnly a, DateOnly b) => a > b ? a : b;

    public static IEnumerable<DateOnly> EachDay(DateOnly start, DateOnly end)
    {
        if (end < start)
            throw new ArgumentException("End cannot be before start");
        var cursor = start;
        while (cursor <= end)
        {
            yield return cursor;
            cursor = cursor.AddDays(1);
        }
    }

    public static string Humanize(DateOnly from, DateOnly to)
    {
        var days = Math.Abs(DaysBetween(from, to));
        if (days == 0) return "today";
        if (days == 1) return "1 day";
        if (days < 7) return $"{days} days";
        if (days < 30) return $"{days / 7} weeks";
        if (days < 365) return $"{days / 30} months";
        return $"{days / 365} years";
    }

    public static bool IsLeapYear(int year) =>
        (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);

    public static int DaysInMonth(int year, int month) => DateTime.DaysInMonth(year, month);

    public static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    public static DateOnly AddBusinessDays(int days) => AddBusinessDays(Today(), days);

    public static bool IsFirstOfMonth(DateOnly date) => date.Day == 1;

    public static bool IsLastOfMonth(DateOnly date) => date == EndOfMonth(date);

    public static int QuarterOf(DateOnly date) => ((date.Month - 1) / 3) + 1;

    public static DateOnly StartOfQuarter(DateOnly date)
    {
        var startMonth = ((QuarterOf(date) - 1) * 3) + 1;
        return new DateOnly(date.Year, startMonth, 1);
    }

    public static DateOnly EndOfQuarter(DateOnly date)
    {
        var endMonth = QuarterOf(date) * 3;
        return new DateOnly(date.Year, endMonth, DateTime.DaysInMonth(date.Year, endMonth));
    }
}
