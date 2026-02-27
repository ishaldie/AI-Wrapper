using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class AgencyDataImporter : IAgencyDataImporter
{
    private readonly AppDbContext _db;
    private readonly ILogger<AgencyDataImporter> _logger;
    private readonly IEdgarCmbsClient? _edgarClient;

    private const int BatchSize = 5000;

    /// <summary>
    /// Maps Fannie Mae / Freddie Mac property type strings to app PropertyType enum.
    /// </summary>
    private static readonly Dictionary<string, PropertyType> AgencyPropertyTypeMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Multifamily"] = PropertyType.Multifamily,
            ["MF"] = PropertyType.Multifamily,
            ["Senior Housing"] = PropertyType.SeniorApartment,
            ["Assisted Living"] = PropertyType.AssistedLiving,
            ["Skilled Nursing"] = PropertyType.SkilledNursing,
            ["Memory Care"] = PropertyType.MemoryCare,
            ["Independent Living"] = PropertyType.IndependentLiving,
            ["CCRC"] = PropertyType.CCRC,
            ["Manufactured Housing"] = PropertyType.Multifamily,
            ["LIHTC"] = PropertyType.LIHTC,
            ["Student Housing"] = PropertyType.Multifamily,
            ["Cooperative"] = PropertyType.Multifamily,
        };

    public AgencyDataImporter(AppDbContext db, ILogger<AgencyDataImporter> logger,
        IEdgarCmbsClient? edgarClient = null)
    {
        _db = db;
        _logger = logger;
        _edgarClient = edgarClient;
    }

    public async Task<int> ImportFannieMaeCsvAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        // Load existing keys for dedup (Source + DealName/SecuritizationId + LoanAmount + OriginationDate)
        var existingKeys = await LoadExistingKeys(SecuritizationDataSource.FannieMae, cancellationToken);

        var comps = new List<SecuritizationComp>();
        var added = 0;
        using var reader = new StreamReader(csvStream);

        // Read header line
        var header = await reader.ReadLineAsync(cancellationToken);
        if (header is null) return 0;

        var columns = header.Split(',');
        var colMap = BuildColumnMap(columns);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var fields = line.Split(',');

            var propertyTypeStr = GetField(fields, colMap, "property_type");
            var propertyType = MapPropertyType(propertyTypeStr);
            var loanAmount = ParseDecimal(GetField(fields, colMap, "original_upb"));
            var originationDate = ParseDate(GetField(fields, colMap, "origination_date"));
            var poolNumber = GetField(fields, colMap, "pool_number");

            // Skip duplicates
            var key = MakeKey(poolNumber, loanAmount, originationDate);
            if (existingKeys.Contains(key)) continue;
            existingKeys.Add(key);

            var comp = new SecuritizationComp(SecuritizationDataSource.FannieMae)
            {
                PropertyType = propertyType,
                State = GetField(fields, colMap, "state"),
                City = GetField(fields, colMap, "city"),
                MSA = GetField(fields, colMap, "msa"),
                Units = ParseInt(GetField(fields, colMap, "units")),
                LoanAmount = loanAmount,
                InterestRate = ParseDecimal(GetField(fields, colMap, "note_rate")),
                DSCR = ParseDecimal(GetField(fields, colMap, "dscr")),
                LTV = ParseDecimal(GetField(fields, colMap, "ltv")),
                Occupancy = ParseDecimal(GetField(fields, colMap, "occupancy_rate")),
                OriginationDate = originationDate,
                MaturityDate = ParseDate(GetField(fields, colMap, "maturity_date")),
                SecuritizationId = poolNumber,
            };

            comps.Add(comp);

            if (comps.Count >= BatchSize)
            {
                await _db.SecuritizationComps.AddRangeAsync(comps, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                added += comps.Count;
                comps.Clear();
            }
        }

        if (comps.Count > 0)
        {
            await _db.SecuritizationComps.AddRangeAsync(comps, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            added += comps.Count;
        }

        _logger.LogInformation("Imported {Added} new Fannie Mae comps (skipped duplicates)", added);
        return added;
    }

    public async Task<int> ImportFreddieMacCsvAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        var existingKeys = await LoadExistingKeys(SecuritizationDataSource.FreddieMac, cancellationToken);

        var comps = new List<SecuritizationComp>();
        var added = 0;
        using var reader = new StreamReader(csvStream);

        var header = await reader.ReadLineAsync(cancellationToken);
        if (header is null) return 0;

        var columns = header.Split(',');
        var colMap = BuildColumnMap(columns);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var fields = line.Split(',');

            var propertyTypeStr = GetField(fields, colMap, "property_type");
            var propertyType = MapPropertyType(propertyTypeStr);
            var loanAmount = ParseDecimal(GetField(fields, colMap, "original_loan_amount"));
            var originationDate = ParseDate(GetField(fields, colMap, "origination_date"));
            var dealName = GetField(fields, colMap, "deal_name");

            // Skip duplicates
            var key = MakeKey(dealName, loanAmount, originationDate);
            if (existingKeys.Contains(key)) continue;
            existingKeys.Add(key);

            var comp = new SecuritizationComp(SecuritizationDataSource.FreddieMac)
            {
                PropertyType = propertyType,
                State = GetField(fields, colMap, "property_state"),
                City = GetField(fields, colMap, "property_city"),
                MSA = GetField(fields, colMap, "metropolitan_area"),
                Units = ParseInt(GetField(fields, colMap, "number_of_units")),
                LoanAmount = loanAmount,
                InterestRate = ParseDecimal(GetField(fields, colMap, "note_rate")),
                DSCR = ParseDecimal(GetField(fields, colMap, "dscr_at_origination")),
                LTV = ParseDecimal(GetField(fields, colMap, "ltv_at_origination")),
                Occupancy = ParseDecimal(GetField(fields, colMap, "occupancy")),
                OriginationDate = originationDate,
                MaturityDate = ParseDate(GetField(fields, colMap, "maturity_date")),
                DealName = dealName,
            };

            comps.Add(comp);

            if (comps.Count >= BatchSize)
            {
                await _db.SecuritizationComps.AddRangeAsync(comps, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                added += comps.Count;
                comps.Clear();
            }
        }

        if (comps.Count > 0)
        {
            await _db.SecuritizationComps.AddRangeAsync(comps, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            added += comps.Count;
        }

        _logger.LogInformation("Imported {Added} new Freddie Mac comps (skipped duplicates)", added);
        return added;
    }

    public async Task<int> ImportEdgarCmbsAsync(int monthsBack = 120, CancellationToken cancellationToken = default)
    {
        if (_edgarClient is null)
        {
            _logger.LogWarning("EDGAR client not configured, skipping CMBS import");
            return 0;
        }

        var existingKeys = await LoadExistingKeys(SecuritizationDataSource.CMBS, cancellationToken);

        var comps = await _edgarClient.FetchRecentFilingsAsync(monthsBack, cancellationToken);

        // Batch insert — skip duplicates
        var batch = new List<SecuritizationComp>();
        var added = 0;
        foreach (var comp in comps)
        {
            var key = MakeKey(comp.DealName ?? comp.SecuritizationId, comp.LoanAmount, comp.OriginationDate);
            if (existingKeys.Contains(key)) continue;
            existingKeys.Add(key);

            batch.Add(comp);
            if (batch.Count >= BatchSize)
            {
                await _db.SecuritizationComps.AddRangeAsync(batch, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                added += batch.Count;
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await _db.SecuritizationComps.AddRangeAsync(batch, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            added += batch.Count;
        }

        _logger.LogInformation("Imported {Added} new CMBS comps from EDGAR (skipped duplicates)", added);
        return added;
    }

    /// <summary>
    /// Loads existing comp fingerprints for a given source so we can skip duplicates on re-import.
    /// Key = DealName|LoanAmount|OriginationDate — cheap in-memory dedup.
    /// </summary>
    private async Task<HashSet<string>> LoadExistingKeys(SecuritizationDataSource source, CancellationToken ct)
    {
        var rows = await _db.SecuritizationComps
            .Where(c => c.Source == source)
            .Select(c => new { c.DealName, c.SecuritizationId, c.LoanAmount, c.OriginationDate })
            .ToListAsync(ct);

        var keys = new HashSet<string>(rows.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
            keys.Add(MakeKey(r.DealName ?? r.SecuritizationId, r.LoanAmount, r.OriginationDate));
        return keys;
    }

    private static string MakeKey(string? name, decimal? amount, DateTime? date)
        => $"{name ?? ""}|{amount?.ToString("F0") ?? ""}|{date?.ToString("yyyy-MM-dd") ?? ""}";

    private static Dictionary<string, int> BuildColumnMap(string[] columns)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < columns.Length; i++)
            map[columns[i].Trim()] = i;
        return map;
    }

    private static string? GetField(string[] fields, Dictionary<string, int> colMap, string columnName)
    {
        if (!colMap.TryGetValue(columnName, out var index) || index >= fields.Length)
            return null;
        var value = fields[index].Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static PropertyType? MapPropertyType(string? propertyTypeStr)
    {
        if (string.IsNullOrEmpty(propertyTypeStr)) return null;
        return AgencyPropertyTypeMap.TryGetValue(propertyTypeStr, out var pt) ? pt : null;
    }

    private static int? ParseInt(string? value)
        => int.TryParse(value, out var result) ? result : null;

    private static decimal? ParseDecimal(string? value)
        => decimal.TryParse(value, out var result) ? result : null;

    private static DateTime? ParseDate(string? value)
        => DateTime.TryParse(value, out var result) ? result : null;
}
