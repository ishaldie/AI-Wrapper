using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class AgencyDataImporterTests : IAsyncLifetime
{
    private readonly AppDbContext _db;
    private readonly AgencyDataImporter _importer;

    private const string FannieMaeSampleCsv =
        "loan_id,property_type,state,city,msa,units,original_upb,note_rate,dscr,ltv,origination_date,maturity_date,pool_number,occupancy_rate\n" +
        "FM001,Multifamily,GA,Atlanta,Atlanta-Sandy Springs,120,15000000,5.25,1.35,72.5,2023-06-15,2033-06-01,AL0001,94.5\n" +
        "FM002,Senior Housing,TX,Dallas,Dallas-Fort Worth,80,8500000,6.10,1.22,77.3,2024-01-10,2034-01-10,AL0002,88.0\n" +
        "FM003,Multifamily,CA,Los Angeles,Los Angeles-Long Beach,250,32000000,4.95,1.45,68.0,2023-09-20,2033-09-20,AL0003,96.0\n";

    private const string FreddieMacSampleCsv =
        "loan_identifier,property_type,property_state,property_city,metropolitan_area,number_of_units,original_loan_amount,note_rate,dscr_at_origination,ltv_at_origination,origination_date,maturity_date,deal_name,occupancy\n" +
        "FK001,Multifamily,FL,Miami,Miami-Fort Lauderdale,150,18000000,5.50,1.30,74.0,2023-08-01,2033-08-01,K-150,92.5\n" +
        "FK002,Multifamily,NY,New York,New York-Newark,300,45000000,4.75,1.55,65.0,2024-02-15,2034-02-15,K-151,97.0\n";

    public AgencyDataImporterTests()
    {
        var dbName = $"AgencyDataImporterTests_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        _db = new AppDbContext(options);
        _importer = new AgencyDataImporter(_db, NullLogger<AgencyDataImporter>.Instance);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task ImportFannieMaeCsv_ParsesAllRows()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(FannieMaeSampleCsv));
        var count = await _importer.ImportFannieMaeCsvAsync(stream);

        Assert.Equal(3, count);
        Assert.Equal(3, await _db.SecuritizationComps.CountAsync());
    }

    [Fact]
    public async Task ImportFannieMaeCsv_SetsSourceToFannieMae()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(FannieMaeSampleCsv));
        await _importer.ImportFannieMaeCsvAsync(stream);

        var comps = await _db.SecuritizationComps.ToListAsync();
        Assert.All(comps, c => Assert.Equal(SecuritizationDataSource.FannieMae, c.Source));
    }

    [Fact]
    public async Task ImportFannieMaeCsv_MapsFieldsCorrectly()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(FannieMaeSampleCsv));
        await _importer.ImportFannieMaeCsvAsync(stream);

        var comp = await _db.SecuritizationComps
            .FirstAsync(c => c.State == "GA");

        Assert.Equal(PropertyType.Multifamily, comp.PropertyType);
        Assert.Equal("Atlanta", comp.City);
        Assert.Equal("Atlanta-Sandy Springs", comp.MSA);
        Assert.Equal(120, comp.Units);
        Assert.Equal(15_000_000m, comp.LoanAmount);
        Assert.Equal(5.25m, comp.InterestRate);
        Assert.Equal(1.35m, comp.DSCR);
        Assert.Equal(72.5m, comp.LTV);
        Assert.Equal(94.5m, comp.Occupancy);
        Assert.Equal(new DateTime(2023, 6, 15), comp.OriginationDate);
        Assert.Equal(new DateTime(2033, 6, 1), comp.MaturityDate);
        Assert.Equal("AL0001", comp.SecuritizationId);
    }

    [Fact]
    public async Task ImportFannieMaeCsv_MapsSeniorHousing()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(FannieMaeSampleCsv));
        await _importer.ImportFannieMaeCsvAsync(stream);

        var comp = await _db.SecuritizationComps
            .FirstAsync(c => c.State == "TX");

        Assert.Equal(PropertyType.SeniorApartment, comp.PropertyType);
    }

    [Fact]
    public async Task ImportFannieMaeCsv_EmptyStream_ReturnsZero()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("loan_id,property_type,state\n"));
        var count = await _importer.ImportFannieMaeCsvAsync(stream);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ImportFreddieMacCsv_ParsesAllRows()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(FreddieMacSampleCsv));
        var count = await _importer.ImportFreddieMacCsvAsync(stream);

        Assert.Equal(2, count);
        Assert.Equal(2, await _db.SecuritizationComps.CountAsync());
    }

    [Fact]
    public async Task ImportFreddieMacCsv_SetsSourceToFreddieMac()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(FreddieMacSampleCsv));
        await _importer.ImportFreddieMacCsvAsync(stream);

        var comps = await _db.SecuritizationComps.ToListAsync();
        Assert.All(comps, c => Assert.Equal(SecuritizationDataSource.FreddieMac, c.Source));
    }

    [Fact]
    public async Task ImportFreddieMacCsv_MapsFieldsCorrectly()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(FreddieMacSampleCsv));
        await _importer.ImportFreddieMacCsvAsync(stream);

        var comp = await _db.SecuritizationComps
            .FirstAsync(c => c.State == "FL");

        Assert.Equal(PropertyType.Multifamily, comp.PropertyType);
        Assert.Equal("Miami", comp.City);
        Assert.Equal("Miami-Fort Lauderdale", comp.MSA);
        Assert.Equal(150, comp.Units);
        Assert.Equal(18_000_000m, comp.LoanAmount);
        Assert.Equal(5.50m, comp.InterestRate);
        Assert.Equal(1.30m, comp.DSCR);
        Assert.Equal(74.0m, comp.LTV);
        Assert.Equal(92.5m, comp.Occupancy);
        Assert.Equal("K-150", comp.DealName);
    }

    [Fact]
    public async Task ImportFreddieMacCsv_EmptyStream_ReturnsZero()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("loan_identifier,property_type\n"));
        var count = await _importer.ImportFreddieMacCsvAsync(stream);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ImportFannieMaeCsv_ClearsExistingSourceData()
    {
        // Import once
        var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(FannieMaeSampleCsv));
        await _importer.ImportFannieMaeCsvAsync(stream1);
        Assert.Equal(3, await _db.SecuritizationComps.CountAsync());

        // Import again — should replace, not duplicate
        var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(FannieMaeSampleCsv));
        await _importer.ImportFannieMaeCsvAsync(stream2);
        Assert.Equal(3, await _db.SecuritizationComps.CountAsync());
    }
}
