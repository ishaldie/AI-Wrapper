using System.Text;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.DTOs.Report;

namespace ZSR.Underwriting.Application.Services;

public static class MarketDataEnricher
{
    public static PropertyCompsSection EnrichPropertyComps(MarketContextDto marketContext)
    {
        var comps = marketContext.ComparableTransactions;

        if (comps == null || comps.Count == 0)
        {
            return new PropertyCompsSection
            {
                Narrative = "Comparable transaction data is currently unavailable for this market."
            };
        }

        var sb = new StringBuilder();
        sb.Append("Based on recent market transactions, ");
        sb.Append($"{comps.Count} comparable sale(s) were identified. ");

        foreach (var comp in comps)
        {
            sb.Append($"{comp.Name}: {comp.Description}. ");
        }

        return new PropertyCompsSection
        {
            Narrative = sb.ToString().TrimEnd()
        };
    }

    public static TenantMarketSection EnrichTenantMarket(
        MarketContextDto marketContext,
        decimal subjectRentPerUnit,
        decimal subjectOccupancy,
        TenantDemographicsDto? demographics = null)
    {
        var employers = marketContext.MajorEmployers;
        var drivers = marketContext.EconomicDrivers;
        var pipeline = marketContext.ConstructionPipeline;

        bool hasData = (employers != null && employers.Count > 0)
                    || (drivers != null && drivers.Count > 0)
                    || (pipeline != null && pipeline.Count > 0);

        if (!hasData && demographics == null)
        {
            return new TenantMarketSection
            {
                Narrative = "Market intelligence data is currently unavailable for this area.",
                SubjectRentPerUnit = subjectRentPerUnit,
                SubjectOccupancy = subjectOccupancy
            };
        }

        var sb = new StringBuilder();

        if (employers != null && employers.Count > 0)
        {
            sb.Append("Major employers in the area include ");
            sb.Append(string.Join(", ", employers.Select(e => e.Name)));
            sb.Append(". ");
        }

        if (drivers != null && drivers.Count > 0)
        {
            sb.Append("Key economic drivers: ");
            sb.Append(string.Join("; ", drivers.Select(d => $"{d.Name} — {d.Description}")));
            sb.Append(". ");
        }

        if (pipeline != null && pipeline.Count > 0)
        {
            sb.Append($"Construction pipeline includes {pipeline.Count} project(s): ");
            sb.Append(string.Join("; ", pipeline.Select(p => $"{p.Name} — {p.Description}")));
            sb.Append(". ");
        }

        // Build benchmarks from Census demographics
        var benchmarks = new List<BenchmarkRow>();
        decimal marketRent = 0m;

        if (demographics != null)
        {
            sb.Append($"(Source: {demographics.Source}) ");

            if (demographics.MedianHouseholdIncome > 0)
            {
                benchmarks.Add(new BenchmarkRow
                {
                    Metric = "Median Household Income",
                    Subject = "N/A",
                    Market = $"${demographics.MedianHouseholdIncome:N0}",
                    Variance = "N/A"
                });
            }

            if (demographics.MedianGrossRent > 0)
            {
                marketRent = demographics.MedianGrossRent;
                var variance = subjectRentPerUnit > 0
                    ? Math.Round((subjectRentPerUnit - marketRent) / marketRent * 100m, 1)
                    : 0m;
                benchmarks.Add(new BenchmarkRow
                {
                    Metric = "Median Gross Rent",
                    Subject = $"${subjectRentPerUnit:N0}",
                    Market = $"${marketRent:N0}",
                    Variance = $"{(variance >= 0 ? "+" : "")}{variance}%"
                });
            }

            if (demographics.AverageHouseholdSize > 0)
            {
                benchmarks.Add(new BenchmarkRow
                {
                    Metric = "Avg Household Size",
                    Subject = "N/A",
                    Market = demographics.AverageHouseholdSize.ToString("F2"),
                    Variance = "N/A"
                });
            }

            if (demographics.RentBurdenPercent > 0)
            {
                benchmarks.Add(new BenchmarkRow
                {
                    Metric = "Rent Burden (>=30% HHI)",
                    Subject = "N/A",
                    Market = $"{demographics.RentBurdenPercent}%",
                    Variance = "N/A"
                });
            }
        }

        return new TenantMarketSection
        {
            Narrative = sb.ToString().TrimEnd(),
            SubjectRentPerUnit = subjectRentPerUnit,
            SubjectOccupancy = subjectOccupancy,
            MarketRentPerUnit = marketRent,
            Benchmarks = benchmarks
        };
    }

    public static decimal? GetEffectiveLoanRate(decimal? userRate, MarketContextDto marketContext)
    {
        if (userRate.HasValue)
            return userRate.Value;

        if (marketContext.CurrentFannieMaeRate.HasValue)
            return marketContext.CurrentFannieMaeRate.Value;

        return null;
    }

    public static List<string> BuildSourceAttribution(MarketContextDto marketContext)
    {
        var attributions = new List<string>();

        if (marketContext.SourceUrls == null || marketContext.SourceUrls.Count == 0)
            return attributions;

        foreach (var (category, urls) in marketContext.SourceUrls)
        {
            foreach (var url in urls)
            {
                attributions.Add($"[{category}] {url}");
            }
        }

        return attributions;
    }
}
