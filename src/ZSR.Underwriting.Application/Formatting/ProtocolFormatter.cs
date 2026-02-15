using System.Globalization;

namespace ZSR.Underwriting.Application.Formatting;

/// <summary>
/// Protocol-compliant formatting for underwriting report output.
/// Rules: $X,XXX (currency), XX.X% (percentages), X.XXx (multiples).
/// </summary>
public static class ProtocolFormatter
{
    private static readonly CultureInfo US = CultureInfo.GetCultureInfo("en-US");

    /// <summary>Currency rounded to whole dollars: $1,234,567</summary>
    public static string Currency(decimal value)
    {
        var rounded = Math.Round(value, 0, MidpointRounding.AwayFromZero);
        return rounded.ToString("C0", US);
    }

    /// <summary>Currency with two decimals: $1,234.56</summary>
    public static string CurrencyExact(decimal value)
    {
        return value.ToString("C2", US);
    }

    /// <summary>Percentage with one decimal: 95.0%</summary>
    public static string Percent(decimal value)
    {
        var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);
        return $"{rounded.ToString("F1", US)}%";
    }

    /// <summary>Percentage with two decimals: 6.75%</summary>
    public static string PercentExact(decimal value)
    {
        var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        return $"{rounded.ToString("F2", US)}%";
    }

    /// <summary>Multiple with two decimals: 1.25x</summary>
    public static string Multiple(decimal value)
    {
        var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        return $"{rounded.ToString("F2", US)}x";
    }

    /// <summary>Currency per unit: $12,000/unit</summary>
    public static string PerUnit(decimal total, int units)
    {
        if (units == 0) return "N/A";
        var perUnit = Math.Round(total / units, 0, MidpointRounding.AwayFromZero);
        return $"{perUnit.ToString("C0", US)}/unit";
    }

    /// <summary>Currency per square foot: $20.00/SF</summary>
    public static string PerSf(decimal total, int squareFeet)
    {
        if (squareFeet == 0) return "N/A";
        var perSf = total / squareFeet;
        return $"{perSf.ToString("C2", US)}/SF";
    }

    /// <summary>Year count with plural handling: 5 years</summary>
    public static string Years(int years)
    {
        return years == 1 ? "1 year" : $"{years} years";
    }

    /// <summary>Integer with comma separators: 1,234</summary>
    public static string Integer(int value)
    {
        return value.ToString("N0", US);
    }
}
