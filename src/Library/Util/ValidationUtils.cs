using System;
using System.Text.RegularExpressions;

namespace Library.Util;

public static class ValidationUtils
{
    private static readonly Regex EmailRegex =
        new(@"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex =
        new(@"^\+?[0-9 .()-]{7,20}$", RegexOptions.Compiled);
    private static readonly Regex Isbn10Regex =
        new("^[0-9]{9}[0-9Xx]$", RegexOptions.Compiled);
    private static readonly Regex Isbn13Regex =
        new("^[0-9]{13}$", RegexOptions.Compiled);
    private static readonly Regex UrlRegex =
        new(@"^https?://[\w.-]+(:[0-9]+)?(/.*)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex HexColorRegex =
        new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);
    private static readonly Regex PostalCodeRegex =
        new("^[A-Za-z0-9 -]{3,10}$", RegexOptions.Compiled);

    public static string RequireNonBlank(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{fieldName} cannot be null or blank", fieldName);
        return value;
    }

    public static T RequireNonNull<T>(T? value, string fieldName) where T : class
    {
        if (value is null)
            throw new ArgumentException($"{fieldName} cannot be null", fieldName);
        return value;
    }

    public static int RequirePositive(int value, string fieldName)
    {
        if (value <= 0)
            throw new ArgumentException($"{fieldName} must be positive", fieldName);
        return value;
    }

    public static int RequireNonNegative(int value, string fieldName)
    {
        if (value < 0)
            throw new ArgumentException($"{fieldName} cannot be negative", fieldName);
        return value;
    }

    public static double RequirePositive(double value, string fieldName)
    {
        if (value <= 0)
            throw new ArgumentException($"{fieldName} must be positive", fieldName);
        return value;
    }

    public static double RequireNonNegative(double value, string fieldName)
    {
        if (value < 0)
            throw new ArgumentException($"{fieldName} cannot be negative", fieldName);
        return value;
    }

    public static int RequireInRange(int value, int min, int max, string fieldName)
    {
        if (value < min || value > max)
            throw new ArgumentException(
                $"{fieldName} must be between {min} and {max}, got {value}", fieldName);
        return value;
    }

    public static bool IsValidEmail(string? email) =>
        email is not null && EmailRegex.IsMatch(email);

    public static string RequireEmail(string? email)
    {
        if (!IsValidEmail(email))
            throw new ArgumentException($"Invalid email: {email}", nameof(email));
        return email!;
    }

    public static bool IsValidPhone(string? phone) =>
        phone is not null && PhoneRegex.IsMatch(phone);

    public static string RequirePhone(string? phone)
    {
        if (!IsValidPhone(phone))
            throw new ArgumentException($"Invalid phone: {phone}", nameof(phone));
        return phone!;
    }

    public static bool IsValidIsbn10(string? isbn)
    {
        if (isbn is null) return false;
        var cleaned = isbn.Replace("-", string.Empty);
        if (!Isbn10Regex.IsMatch(cleaned)) return false;
        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (cleaned[i] - '0') * (10 - i);
        var last = cleaned[9];
        sum += (last == 'X' || last == 'x') ? 10 : (last - '0');
        return sum % 11 == 0;
    }

    public static bool IsValidIsbn13(string? isbn)
    {
        if (isbn is null) return false;
        var cleaned = isbn.Replace("-", string.Empty);
        if (!Isbn13Regex.IsMatch(cleaned)) return false;
        var sum = 0;
        for (var i = 0; i < 13; i++)
        {
            var digit = cleaned[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }
        return sum % 10 == 0;
    }

    public static bool IsValidIsbn(string? isbn) =>
        IsValidIsbn10(isbn) || IsValidIsbn13(isbn);

    public static string RequireIsbn(string? isbn)
    {
        if (!IsValidIsbn(isbn))
            throw new ArgumentException($"Invalid ISBN: {isbn}", nameof(isbn));
        return isbn!;
    }

    public static bool IsValidUrl(string? url) =>
        url is not null && UrlRegex.IsMatch(url);

    public static bool IsValidHexColor(string? hex) =>
        hex is not null && HexColorRegex.IsMatch(hex);

    public static bool IsValidPostalCode(string? code) =>
        code is not null && PostalCodeRegex.IsMatch(code);

    public static string? RequireMaxLength(string? value, int maxLength, string fieldName)
    {
        if (value is null) return null;
        if (value.Length > maxLength)
            throw new ArgumentException(
                $"{fieldName} cannot exceed {maxLength} characters", fieldName);
        return value;
    }

    public static string RequireMinLength(string? value, int minLength, string fieldName)
    {
        if (value is null || value.Length < minLength)
            throw new ArgumentException(
                $"{fieldName} must be at least {minLength} characters", fieldName);
        return value;
    }

    public static string? Sanitize(string? value)
    {
        if (value is null) return null;
        return System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"\s+", " ");
    }

    public static bool IsAlphanumeric(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        foreach (var c in value)
            if (!char.IsLetterOrDigit(c)) return false;
        return true;
    }

    public static bool IsNumeric(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        foreach (var c in value)
            if (!char.IsDigit(c)) return false;
        return true;
    }

    public static bool HasSpecialChar(string? value)
    {
        if (value is null) return false;
        foreach (var c in value)
            if (!char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c))
                return true;
        return false;
    }
}
