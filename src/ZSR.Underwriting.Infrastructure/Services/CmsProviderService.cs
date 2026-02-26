using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;

namespace ZSR.Underwriting.Infrastructure.Services;

public class CmsProviderService : ICmsProviderService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CmsProviderService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    // CMS Care Compare dataset IDs
    private const string ProviderDatasetId = "4pq5-n9py";
    private const string DeficiencyDatasetId = "r5ix-sfxw";
    private const string PenaltyDatasetId = "g6vv-u9sr";

    public CmsProviderService(HttpClient http, IMemoryCache cache, ILogger<CmsProviderService> logger)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
    }

    public async Task<CmsProviderDto?> SearchByNameAndStateAsync(string facilityName, string state, CancellationToken ct = default)
    {
        var cacheKey = $"cms:search:{facilityName.ToUpperInvariant()}:{state.ToUpperInvariant()}";
        if (_cache.TryGetValue<CmsProviderDto>(cacheKey, out var cached))
            return cached;

        try
        {
            var cleanState = state.Trim().ToUpperInvariant();
            if (cleanState.Length > 2)
                cleanState = cleanState[..2]; // Take first two chars as state abbreviation

            var url = $"{ProviderDatasetId}/0?" +
                $"conditions[0][property]=provider_name&conditions[0][value]={Uri.EscapeDataString(facilityName.ToUpperInvariant())}&conditions[0][operator]=CONTAINS" +
                $"&conditions[1][property]=provider_state&conditions[1][value]={Uri.EscapeDataString(cleanState)}" +
                $"&limit=1";

            var json = await _http.GetStringAsync(url, ct);
            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
                return null;

            var provider = MapProvider(results[0]);

            // Fetch deficiencies and penalties
            if (!string.IsNullOrEmpty(provider.CcnNumber))
            {
                provider.Deficiencies = await GetDeficienciesAsync(provider.CcnNumber, ct);
                provider.Penalties = await GetPenaltiesAsync(provider.CcnNumber, ct);
            }

            _cache.Set(cacheKey, provider, CacheDuration);
            if (!string.IsNullOrEmpty(provider.CcnNumber))
                _cache.Set($"cms:ccn:{provider.CcnNumber}", provider, CacheDuration);

            return provider;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CMS provider search failed for {Name}, {State}", facilityName, state);
            return null;
        }
    }

    public async Task<CmsProviderDto?> GetByCcnAsync(string ccn, CancellationToken ct = default)
    {
        var cacheKey = $"cms:ccn:{ccn}";
        if (_cache.TryGetValue<CmsProviderDto>(cacheKey, out var cached))
            return cached;

        try
        {
            var url = $"{ProviderDatasetId}/0?" +
                $"conditions[0][property]=federal_provider_number&conditions[0][value]={Uri.EscapeDataString(ccn)}" +
                $"&limit=1";

            var json = await _http.GetStringAsync(url, ct);
            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
                return null;

            var provider = MapProvider(results[0]);
            provider.Deficiencies = await GetDeficienciesAsync(ccn, ct);
            provider.Penalties = await GetPenaltiesAsync(ccn, ct);

            _cache.Set(cacheKey, provider, CacheDuration);
            return provider;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CMS provider lookup failed for CCN {Ccn}", ccn);
            return null;
        }
    }

    public async Task<List<CmsDeficiencyDto>> GetDeficienciesAsync(string ccn, CancellationToken ct = default)
    {
        try
        {
            var url = $"{DeficiencyDatasetId}/0?" +
                $"conditions[0][property]=federal_provider_number&conditions[0][value]={Uri.EscapeDataString(ccn)}" +
                $"&limit=50&sort[0][property]=survey_date&sort[0][order]=desc";

            var json = await _http.GetStringAsync(url, ct);
            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("results", out var results))
                return [];

            return results.EnumerateArray().Select(r => new CmsDeficiencyDto
            {
                DeficiencyTag = GetString(r, "deficiency_tag_number"),
                Description = GetString(r, "deficiency_description"),
                Scope = GetString(r, "scope_severity"),
                Severity = GetString(r, "deficiency_severity"),
                SurveyDate = GetDate(r, "survey_date"),
                CorrectionDate = GetDate(r, "correction_date"),
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CMS deficiency fetch failed for CCN {Ccn}", ccn);
            return [];
        }
    }

    public async Task<List<CmsPenaltyDto>> GetPenaltiesAsync(string ccn, CancellationToken ct = default)
    {
        try
        {
            var url = $"{PenaltyDatasetId}/0?" +
                $"conditions[0][property]=federal_provider_number&conditions[0][value]={Uri.EscapeDataString(ccn)}" +
                $"&limit=50&sort[0][property]=penalty_date&sort[0][order]=desc";

            var json = await _http.GetStringAsync(url, ct);
            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("results", out var results))
                return [];

            return results.EnumerateArray().Select(r => new CmsPenaltyDto
            {
                PenaltyType = GetString(r, "penalty_type"),
                FineAmount = GetDecimal(r, "fine_amount"),
                PenaltyDate = GetDate(r, "penalty_date"),
                Description = GetStringOrNull(r, "penalty_description"),
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CMS penalty fetch failed for CCN {Ccn}", ccn);
            return [];
        }
    }

    private static CmsProviderDto MapProvider(JsonElement el)
    {
        return new CmsProviderDto
        {
            CcnNumber = GetString(el, "federal_provider_number"),
            ProviderName = GetString(el, "provider_name"),
            OverallRating = GetInt(el, "overall_rating"),
            HealthInspectionRating = GetInt(el, "health_inspection_rating"),
            QualityMeasureRating = GetInt(el, "qm_rating"),
            StaffingRating = GetInt(el, "staffing_rating"),
            CertifiedBeds = GetInt(el, "number_of_certified_beds"),
            AverageResidentsPerDay = GetDecimal(el, "average_number_of_residents_per_day"),
            OwnershipType = GetString(el, "ownership_type"),
            ChainName = GetStringOrNull(el, "associated_with_a_chain"),
            TotalNurseHoursPerResidentDay = GetDecimal(el, "total_nurse_staffing_hours_per_resident_per_day"),
            RnHoursPerResidentDay = GetDecimal(el, "rn_staffing_hours_per_resident_per_day"),
            NursingTurnoverPct = GetDecimal(el, "total_nursing_staff_turnover"),
            TotalDeficiencies = GetInt(el, "total_number_of_health_deficiencies"),
            NumberOfFines = GetInt(el, "number_of_fines"),
            TotalFinesAmount = GetDecimal(el, "total_amount_of_fines_in_dollars"),
            TotalPenalties = GetInt(el, "total_number_of_penalties"),
            AbuseFlag = GetString(el, "abuse_icon").Equals("Y", StringComparison.OrdinalIgnoreCase),
            SpecialFocusStatus = GetStringOrNull(el, "special_focus_status"),
            LastInspectionDate = GetDate(el, "most_recent_health_inspection_more_than_1_year_ago"),
            HealthDeficiencyScore = GetInt(el, "weighted_health_survey_score"),
            DataAsOfDate = DateTime.UtcNow,
        };
    }

    private static string GetString(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) ? v.GetString() ?? string.Empty : string.Empty;

    private static string? GetStringOrNull(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) ? v.GetString() : null;

    private static int GetInt(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return 0;
        if (v.ValueKind == JsonValueKind.Number) return v.GetInt32();
        return int.TryParse(v.GetString(), out var n) ? n : 0;
    }

    private static decimal GetDecimal(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return 0m;
        if (v.ValueKind == JsonValueKind.Number) return v.GetDecimal();
        return decimal.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }

    private static DateTime? GetDate(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return null;
        var str = v.GetString();
        return DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : null;
    }
}
