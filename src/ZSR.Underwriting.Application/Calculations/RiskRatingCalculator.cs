using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Application.Calculations;

public static class RiskRatingCalculator
{
    public static RiskSeverity RateRentPremium(decimal subjectRent, decimal marketRent)
    {
        if (marketRent == 0m) return RiskSeverity.Low;

        var premiumPercent = (subjectRent - marketRent) / marketRent * 100m;

        return premiumPercent switch
        {
            >= 15m => RiskSeverity.Critical,
            >= 10m => RiskSeverity.High,
            >= 5m => RiskSeverity.Moderate,
            _ => RiskSeverity.Low,
        };
    }

    public static RiskSeverity RateDscr(decimal dscr)
    {
        return dscr switch
        {
            < 1.0m => RiskSeverity.Critical,
            < 1.15m => RiskSeverity.High,
            < 1.25m => RiskSeverity.Moderate,
            _ => RiskSeverity.Low,
        };
    }

    public static RiskSeverity RateOccupancyGap(decimal subjectOccupancy, decimal marketOccupancy)
    {
        var gap = marketOccupancy - subjectOccupancy;

        return gap switch
        {
            >= 20m => RiskSeverity.Critical,
            >= 10m => RiskSeverity.High,
            >= 5m => RiskSeverity.Moderate,
            _ => RiskSeverity.Low,
        };
    }

    public static RiskSeverity RateFicoGap(int subjectFico, int metroFico)
    {
        var gap = metroFico - subjectFico;

        return gap switch
        {
            >= 75 => RiskSeverity.Critical,
            >= 50 => RiskSeverity.High,
            >= 25 => RiskSeverity.Moderate,
            _ => RiskSeverity.Low,
        };
    }
}
