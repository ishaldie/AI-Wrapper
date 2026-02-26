using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZSR.Underwriting.Infrastructure.Services;
using ZSR.Underwriting.Tests.Helpers;

namespace ZSR.Underwriting.Tests.Services;

public class CmsProviderServiceTests : IDisposable
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly ILogger<CmsProviderService> _logger = NullLogger<CmsProviderService>.Instance;

    public void Dispose() => _cache.Dispose();

    private CmsProviderService CreateService(string jsonResponse, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler(jsonResponse, statusCode);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://data.cms.gov/provider-data/api/1/datastore/query/")
        };
        return new CmsProviderService(httpClient, _cache, _logger);
    }

    private const string ProviderSearchResponse = """
    {
        "results": [{
            "federal_provider_number": "455001",
            "provider_name": "SUNRISE SENIOR CARE",
            "overall_rating": "4",
            "health_inspection_rating": "3",
            "qm_rating": "5",
            "staffing_rating": "4",
            "number_of_certified_beds": "120",
            "average_number_of_residents_per_day": "105.3",
            "ownership_type": "For profit - Corporation",
            "associated_with_a_chain": "Sunrise Health Corp",
            "total_nurse_staffing_hours_per_resident_per_day": "4.25",
            "rn_staffing_hours_per_resident_per_day": "1.15",
            "total_nursing_staff_turnover": "52.3",
            "total_number_of_health_deficiencies": "8",
            "number_of_fines": "2",
            "total_amount_of_fines_in_dollars": "15000.00",
            "total_number_of_penalties": "3",
            "abuse_icon": "N",
            "special_focus_status": null,
            "most_recent_health_inspection_more_than_1_year_ago": "2025-06-15",
            "weighted_health_survey_score": "42",
            "provider_state": "TX"
        }]
    }
    """;

    private const string EmptyResponse = """{ "results": [] }""";

    private const string DeficienciesResponse = """
    {
        "results": [{
            "deficiency_tag_number": "F689",
            "deficiency_description": "Free of Accident Hazards",
            "scope_severity": "D",
            "deficiency_severity": "Minimal harm",
            "survey_date": "2025-06-15",
            "correction_date": "2025-07-01",
            "federal_provider_number": "455001"
        }, {
            "deficiency_tag_number": "F812",
            "deficiency_description": "Food Procurement/Storage",
            "scope_severity": "E",
            "deficiency_severity": "No actual harm",
            "survey_date": "2025-06-15",
            "correction_date": "2025-07-10",
            "federal_provider_number": "455001"
        }]
    }
    """;

    private const string PenaltiesResponse = """
    {
        "results": [{
            "penalty_type": "Fine",
            "fine_amount": "10000.00",
            "penalty_date": "2025-08-01",
            "penalty_description": "Health deficiency fine",
            "federal_provider_number": "455001"
        }]
    }
    """;

    [Fact]
    public async Task SearchByNameAndState_ReturnsProviderData()
    {
        var service = CreateService(ProviderSearchResponse);
        var result = await service.SearchByNameAndStateAsync("Sunrise Senior Care", "TX");

        Assert.NotNull(result);
        Assert.Equal("455001", result!.CcnNumber);
        Assert.Equal("SUNRISE SENIOR CARE", result.ProviderName);
        Assert.Equal(4, result.OverallRating);
        Assert.Equal(3, result.HealthInspectionRating);
        Assert.Equal(5, result.QualityMeasureRating);
        Assert.Equal(4, result.StaffingRating);
        Assert.Equal(120, result.CertifiedBeds);
        Assert.Equal(105.3m, result.AverageResidentsPerDay);
        Assert.Equal("For profit - Corporation", result.OwnershipType);
        Assert.Equal("Sunrise Health Corp", result.ChainName);
    }

    [Fact]
    public async Task SearchByNameAndState_MapsStaffingMetrics()
    {
        var service = CreateService(ProviderSearchResponse);
        var result = await service.SearchByNameAndStateAsync("Sunrise Senior Care", "TX");

        Assert.NotNull(result);
        Assert.Equal(4.25m, result!.TotalNurseHoursPerResidentDay);
        Assert.Equal(1.15m, result.RnHoursPerResidentDay);
        Assert.Equal(52.3m, result.NursingTurnoverPct);
    }

    [Fact]
    public async Task SearchByNameAndState_MapsComplianceMetrics()
    {
        var service = CreateService(ProviderSearchResponse);
        var result = await service.SearchByNameAndStateAsync("Sunrise Senior Care", "TX");

        Assert.NotNull(result);
        Assert.Equal(8, result!.TotalDeficiencies);
        Assert.Equal(2, result.NumberOfFines);
        Assert.Equal(15000m, result.TotalFinesAmount);
        Assert.Equal(3, result.TotalPenalties);
        Assert.False(result.AbuseFlag);
    }

    [Fact]
    public async Task SearchByNameAndState_NoResults_ReturnsNull()
    {
        var service = CreateService(EmptyResponse);
        var result = await service.SearchByNameAndStateAsync("Nonexistent Facility", "ZZ");

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchByNameAndState_HttpError_ReturnsNull()
    {
        var service = CreateService("Server Error", HttpStatusCode.InternalServerError);
        var result = await service.SearchByNameAndStateAsync("Test", "TX");

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchByNameAndState_CachesResult()
    {
        var service = CreateService(ProviderSearchResponse);

        var first = await service.SearchByNameAndStateAsync("Sunrise Senior Care", "TX");
        Assert.NotNull(first);

        // Second call should return cached result even though handler only responds once
        var second = await service.SearchByNameAndStateAsync("Sunrise Senior Care", "TX");
        Assert.NotNull(second);
        Assert.Equal(first!.CcnNumber, second!.CcnNumber);
    }

    [Fact]
    public async Task GetByCcn_ReturnsProviderData()
    {
        var service = CreateService(ProviderSearchResponse);
        var result = await service.GetByCcnAsync("455001");

        Assert.NotNull(result);
        Assert.Equal("455001", result!.CcnNumber);
        Assert.Equal(4, result.OverallRating);
    }

    [Fact]
    public async Task GetByCcn_NoResults_ReturnsNull()
    {
        var service = CreateService(EmptyResponse);
        var result = await service.GetByCcnAsync("000000");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDeficiencies_ReturnsDeficiencyList()
    {
        var service = CreateService(DeficienciesResponse);
        var deficiencies = await service.GetDeficienciesAsync("455001");

        Assert.Equal(2, deficiencies.Count);
        Assert.Equal("F689", deficiencies[0].DeficiencyTag);
        Assert.Equal("Free of Accident Hazards", deficiencies[0].Description);
        Assert.Equal("D", deficiencies[0].Scope);
        Assert.Equal("Minimal harm", deficiencies[0].Severity);
        Assert.Equal(new DateTime(2025, 6, 15), deficiencies[0].SurveyDate);
        Assert.Equal(new DateTime(2025, 7, 1), deficiencies[0].CorrectionDate);
    }

    [Fact]
    public async Task GetPenalties_ReturnsPenaltyList()
    {
        var service = CreateService(PenaltiesResponse);
        var penalties = await service.GetPenaltiesAsync("455001");

        Assert.Single(penalties);
        Assert.Equal("Fine", penalties[0].PenaltyType);
        Assert.Equal(10000m, penalties[0].FineAmount);
        Assert.Equal(new DateTime(2025, 8, 1), penalties[0].PenaltyDate);
        Assert.Equal("Health deficiency fine", penalties[0].Description);
    }

    [Fact]
    public async Task GetDeficiencies_EmptyResults_ReturnsEmptyList()
    {
        var service = CreateService(EmptyResponse);
        var deficiencies = await service.GetDeficienciesAsync("000000");

        Assert.Empty(deficiencies);
    }

    [Fact]
    public async Task GetPenalties_HttpError_ReturnsEmptyList()
    {
        var service = CreateService("error", HttpStatusCode.InternalServerError);
        var penalties = await service.GetPenaltiesAsync("455001");

        Assert.Empty(penalties);
    }
}
