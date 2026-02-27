using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Infrastructure.Services;

public class EdgarCmbsClient : IEdgarCmbsClient
{
    private readonly HttpClient _http;
    private readonly ILogger<EdgarCmbsClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Maps CMBS property type codes to app PropertyType enum.
    /// Only includes codes relevant to multifamily/senior housing.
    /// Other codes (OF=Office, RT=Retail, etc.) are excluded from import.
    /// </summary>
    private static readonly Dictionary<string, PropertyType> PropertyTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MF"] = PropertyType.Multifamily,
        ["HC"] = PropertyType.SeniorApartment,   // Healthcare/Senior Housing
        ["MH"] = PropertyType.Multifamily,        // Manufactured Housing
    };

    public EdgarCmbsClient(HttpClient http, ILogger<EdgarCmbsClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SecuritizationComp>> FetchRecentFilingsAsync(
        int monthsBack = 120,
        CancellationToken cancellationToken = default)
    {
        var comps = new List<SecuritizationComp>();

        try
        {
            var startDate = DateTime.UtcNow.AddMonths(-monthsBack).ToString("yyyy-MM-dd");
            var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

            // Search EDGAR EFTS for ABS-EE filings
            var searchUrl = $"LATEST/search-index?q=%22ABS-EE%22&forms=ABS-EE" +
                            $"&dateRange=custom&startdt={startDate}&enddt={endDate}" +
                            "&from=0&size=100";

            var response = await _http.GetAsync(searchUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("EDGAR EFTS search returned {StatusCode}", response.StatusCode);
                return comps;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var searchResult = JsonSerializer.Deserialize<EdgarSearchResult>(json, JsonOptions);

            if (searchResult?.Hits?.Hits is null || searchResult.Hits.Hits.Count == 0)
                return comps;

            foreach (var hit in searchResult.Hits.Hits)
            {
                try
                {
                    var filingUrl = hit.Id;
                    if (string.IsNullOrEmpty(filingUrl)) continue;

                    // Fetch the EX-102 XML document
                    var xmlResponse = await _http.GetAsync(filingUrl, cancellationToken);
                    if (!xmlResponse.IsSuccessStatusCode) continue;

                    var xml = await xmlResponse.Content.ReadAsStringAsync(cancellationToken);
                    var dealName = hit.Source?.DisplayNames?.FirstOrDefault();
                    var parsed = ParseEx102Xml(xml, dealName);
                    comps.AddRange(parsed);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse EDGAR filing {FilingId}", hit.Id);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "EDGAR EFTS search failed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error fetching EDGAR filings");
        }

        return comps;
    }

    public IReadOnlyList<SecuritizationComp> ParseEx102Xml(string xml, string? dealName = null)
    {
        var comps = new List<SecuritizationComp>();

        try
        {
            var doc = XDocument.Parse(xml);
            var root = doc.Root;
            if (root is null) return comps;

            // Handle both namespaced and non-namespaced XML
            var ns = root.GetDefaultNamespace();

            var assets = root.Descendants(ns + "asset");
            if (!assets.Any())
            {
                // Try without namespace
                assets = root.Descendants("asset");
            }

            foreach (var asset in assets)
            {
                var propertyTypeCode = GetElementValue(asset, ns, "propertyTypeCode");

                // Only import property types we care about
                if (string.IsNullOrEmpty(propertyTypeCode) ||
                    !PropertyTypeMap.TryGetValue(propertyTypeCode, out var propertyType))
                {
                    continue;
                }

                var comp = new SecuritizationComp(SecuritizationDataSource.CMBS)
                {
                    PropertyType = propertyType,
                    State = GetElementValue(asset, ns, "propertyState"),
                    City = GetElementValue(asset, ns, "propertyCity"),
                    Units = ParseInt(GetElementValue(asset, ns, "numberOfUnitsBedsRooms")),
                    LoanAmount = ParseDecimal(GetElementValue(asset, ns, "originalLoanAmount")),
                    InterestRate = ParseDecimal(GetElementValue(asset, ns, "originalInterestRatePercentage")),
                    DSCR = ParseDecimal(GetElementValue(asset, ns, "mostRecentDebtServiceCoverageNOI")),
                    NOI = ParseDecimal(GetElementValue(asset, ns, "mostRecentNOIAmount")),
                    Occupancy = ParseDecimal(GetElementValue(asset, ns, "mostRecentPhysicalOccupancyPercentage")),
                    OriginationDate = ParseDate(GetElementValue(asset, ns, "originationDate")),
                    MaturityDate = ParseDate(GetElementValue(asset, ns, "maturityDate")),
                    DealName = dealName,
                };

                // Compute LTV = LoanAmount / Valuation * 100
                var valuation = ParseDecimal(GetElementValue(asset, ns, "mostRecentValuationAmount"));
                if (comp.LoanAmount.HasValue && valuation.HasValue && valuation.Value > 0)
                {
                    comp.LTV = Math.Round(comp.LoanAmount.Value / valuation.Value * 100, 1);
                }

                // Compute Cap Rate = NOI / Valuation * 100
                if (comp.NOI.HasValue && valuation.HasValue && valuation.Value > 0)
                {
                    comp.CapRate = Math.Round(comp.NOI.Value / valuation.Value * 100, 1);
                }

                comps.Add(comp);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse EX-102 XML");
        }

        return comps;
    }

    private static string? GetElementValue(XElement parent, XNamespace ns, string localName)
    {
        // Try with namespace first, then without
        var value = parent.Element(ns + localName)?.Value;
        if (value is null)
            value = parent.Element(localName)?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int? ParseInt(string? value)
        => int.TryParse(value, out var result) ? result : null;

    private static decimal? ParseDecimal(string? value)
        => decimal.TryParse(value, out var result) ? result : null;

    private static DateTime? ParseDate(string? value)
        => DateTime.TryParse(value, out var result) ? result : null;

    // EDGAR EFTS search response models
    private class EdgarSearchResult
    {
        public EdgarHitsContainer? Hits { get; set; }
    }

    private class EdgarHitsContainer
    {
        public List<EdgarHit>? Hits { get; set; }
    }

    private class EdgarHit
    {
        public string? Id { get; set; }
        public EdgarHitSource? Source { get; set; }
    }

    private class EdgarHitSource
    {
        public List<string>? DisplayNames { get; set; }
    }
}
