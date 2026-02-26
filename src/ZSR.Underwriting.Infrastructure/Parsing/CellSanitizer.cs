namespace ZSR.Underwriting.Infrastructure.Parsing;

/// <summary>
/// Sanitizes cell values to prevent formula injection in CSV/XLSX files.
/// Strips leading =, +, -, @ characters that could trigger formula execution.
/// </summary>
public static class CellSanitizer
{
    private static readonly char[] UnsafeLeadingChars = [' ', '\t', '=', '+', '-', '@'];

    public static string SanitizeCellValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value.TrimStart(UnsafeLeadingChars);
    }
}
