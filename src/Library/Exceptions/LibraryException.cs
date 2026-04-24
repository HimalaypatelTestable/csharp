using System;
using System.Collections.Generic;
using System.Text;

namespace Library.Exceptions;

public class LibraryException : Exception
{
    public enum Severity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public enum Category
    {
        Validation,
        NotFound,
        Conflict,
        BusinessRule,
        Permission,
        Internal,
        External
    }

    private readonly Dictionary<string, object?> _context = new();

    public LibraryException(string message)
        : this(message, null, "LIB_ERR", Severity.Error, Category.Internal) { }

    public LibraryException(string message, Exception? cause)
        : this(message, cause, "LIB_ERR", Severity.Error, Category.Internal) { }

    public LibraryException(string message, string errorCode)
        : this(message, null, errorCode, Severity.Error, Category.Internal) { }

    public LibraryException(string message, string errorCode, Category category)
        : this(message, null, errorCode, Severity.Error, category) { }

    public LibraryException(string message,
                             Exception? cause,
                             string errorCode,
                             Severity severity,
                             Category category)
        : base(message, cause)
    {
        ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? "LIB_ERR" : errorCode;
        SeverityLevel = severity;
        ExceptionCategory = category;
        OccurredAt = DateTimeOffset.UtcNow;
    }

    public string ErrorCode { get; }

    public Severity SeverityLevel { get; }

    public Category ExceptionCategory { get; }

    public DateTimeOffset OccurredAt { get; }

    public IReadOnlyDictionary<string, object?> Context => _context;

    public LibraryException WithContext(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Context key cannot be null or blank", nameof(key));
        _context[key] = value;
        return this;
    }

    public bool IsRetryable =>
        ExceptionCategory == Category.External
        || (SeverityLevel == Severity.Warning && ExceptionCategory != Category.Validation);

    public bool IsClientError =>
        ExceptionCategory == Category.Validation
        || ExceptionCategory == Category.NotFound
        || ExceptionCategory == Category.Conflict
        || ExceptionCategory == Category.Permission
        || ExceptionCategory == Category.BusinessRule;

    public bool IsServerError =>
        ExceptionCategory == Category.Internal || ExceptionCategory == Category.External;

    public string ToJson()
    {
        var sb = new StringBuilder();
        sb.Append('{');
        sb.Append($"\"errorCode\":\"{Escape(ErrorCode)}\",");
        sb.Append($"\"message\":\"{Escape(Message)}\",");
        sb.Append($"\"severity\":\"{SeverityLevel}\",");
        sb.Append($"\"category\":\"{ExceptionCategory}\",");
        sb.Append($"\"occurredAt\":\"{OccurredAt:O}\"");
        if (_context.Count > 0)
        {
            sb.Append(",\"context\":{");
            var first = true;
            foreach (var kv in _context)
            {
                if (!first) sb.Append(',');
                sb.Append($"\"{Escape(kv.Key)}\":\"{Escape(kv.Value?.ToString() ?? string.Empty)}\"");
                first = false;
            }
            sb.Append('}');
        }
        sb.Append('}');
        return sb.ToString();
    }

    private static string Escape(string? raw)
    {
        if (raw is null) return string.Empty;
        return raw.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    public override string ToString() =>
        $"{GetType().Name}[{ErrorCode}:{ExceptionCategory}/{SeverityLevel}] {Message}";

    public static LibraryException NotFound(string resource, string id) =>
        new LibraryException($"{resource} not found: {id}", null,
                             "LIB_NOT_FOUND", Severity.Warning, Category.NotFound)
            .WithContext("resource", resource)
            .WithContext("id", id);

    public static LibraryException Conflict(string message) =>
        new(message, null, "LIB_CONFLICT", Severity.Warning, Category.Conflict);

    public static LibraryException Validation(string message) =>
        new(message, null, "LIB_VALIDATION", Severity.Warning, Category.Validation);

    public static LibraryException BusinessRule(string message) =>
        new(message, null, "LIB_BUSINESS", Severity.Warning, Category.BusinessRule);

    public static LibraryException Permission(string message) =>
        new(message, null, "LIB_PERMISSION", Severity.Error, Category.Permission);

    public static LibraryException Internal(string message, Exception? cause) =>
        new(message, cause, "LIB_INTERNAL", Severity.Critical, Category.Internal);

    public static LibraryException External(string service, string message) =>
        new LibraryException($"External service failure [{service}]: {message}", null,
                              "LIB_EXTERNAL", Severity.Error, Category.External)
            .WithContext("service", service);
}
