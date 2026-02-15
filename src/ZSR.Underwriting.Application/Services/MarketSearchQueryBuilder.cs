using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Application.Services;

public static class MarketSearchQueryBuilder
{
    public static string BuildQuery(string city, string state, MarketSearchCategory category)
    {
        return category switch
        {
            MarketSearchCategory.MajorEmployers =>
                $"largest major employers {city} {state} top companies",
            MarketSearchCategory.ConstructionPipeline =>
                $"multifamily apartment construction pipeline {city} {state} new development",
            MarketSearchCategory.EconomicDrivers =>
                $"economic drivers growth {city} {state} job market economy",
            MarketSearchCategory.Infrastructure =>
                $"infrastructure projects transportation development {city} {state}",
            MarketSearchCategory.ComparableTransactions =>
                $"multifamily apartment sales transactions {city} {state} recent acquisitions",
            MarketSearchCategory.FannieMaeRates =>
                "Fannie Mae multifamily loan interest rates current",
            _ => $"{category} {city} {state}"
        };
    }
}
