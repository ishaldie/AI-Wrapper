namespace ZSR.Underwriting.Infrastructure.Parsing;

/// <summary>
/// Sanitizes cell values to prevent formula injection in CSV/XLSX files.
/// Strips leading =, +, -, @ characters that could trigger formula execution.
/// </summary>
public static class CellSanitizer
{
    private static readonly char[] FormulaChars = ['=', '+', '-', '@'];

    public static string SanitizeCellValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var trimmed = value.TrimStart();
        while (trimmed.Length > 0 && FormulaChars.Contains(trimmed[0]))
        {
            trimmed = trimmed[1..].TrimStart();
        }

        return trimmed;
    }
}
