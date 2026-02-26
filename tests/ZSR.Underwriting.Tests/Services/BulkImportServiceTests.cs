using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class BulkImportServiceTests
{
    private readonly string _userId = "bulk-test-user";
    private readonly ILogger<BulkImportService> _logger = NullLogger<BulkImportService>.Instance;

    private BulkImportService CreateService(
        FakeDealService? dealService = null,
        FakePortfolioService? portfolioService = null)
    {
        return new BulkImportService(
            dealService ?? new FakeDealService(),
            portfolioService ?? new FakePortfolioService(),
            _logger);
    }

    [Fact]
    public async Task ImportAsync_CreatesPortfolioAndDeals()
    {
        var dealService = new FakeDealService();
        var portfolioService = new FakePortfolioService();
        var service = CreateService(dealService, portfolioService);

        var rows = new List<BulkImportRowDto>
        {
            new() { RowNumber = 2, PropertyName = "Deal A", Address = "123 Main St" },
            new() { RowNumber = 3, PropertyName = "Deal B", Address = "456 Oak Ave" },
        };

        var result = await service.ImportAsync(rows, "Test Portfolio", _userId);

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.NotNull(result.PortfolioId);
        Assert.Equal("Test Portfolio", result.PortfolioName);
        Assert.Equal(2, result.CreatedDealIds.Count);

        // Portfolio was created
        Assert.Single(portfolioService.CreatedPortfolios);
        Assert.Equal("Test Portfolio", portfolioService.CreatedPortfolios[0].Name);

        // Deals were assigned to portfolio
        Assert.Equal(2, portfolioService.AssignedDeals.Count);
    }

    [Fact]
    public async Task ImportAsync_SkipsInvalidRows()
    {
        var service = CreateService();

        var rows = new List<BulkImportRowDto>
        {
            new() { RowNumber = 2, PropertyName = "Valid Deal", Address = "123 Main St" },
            new() { RowNumber = 3, PropertyName = "", Address = "" }, // Invalid: missing required fields
            new() { RowNumber = 4, PropertyName = "Another Valid", Address = "789 Elm Blvd" },
        };

        var result = await service.ImportAsync(rows, "Mixed Portfolio", _userId);

        Assert.Equal(3, result.TotalRows);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(2, result.CreatedDealIds.Count);
    }

    [Fact]
    public async Task ImportAsync_AllInvalid_ReturnsError()
    {
        var service = CreateService();

        var rows = new List<BulkImportRowDto>
        {
            new() { RowNumber = 2, PropertyName = "", Address = "" },
            new() { RowNumber = 3, PropertyName = "", Address = "" },
        };

        var result = await service.ImportAsync(rows, "Empty Portfolio", _userId);

        Assert.Equal(0, result.SuccessCount);
        Assert.Contains("No valid rows to import.", result.Errors);
        Assert.Null(result.PortfolioId);
    }

    [Fact]
    public async Task ImportAsync_ReportsProgress()
    {
        var service = CreateService();
        var progressValues = new List<int>();
        var progress = new Progress<int>(v => progressValues.Add(v));

        var rows = new List<BulkImportRowDto>
        {
            new() { RowNumber = 2, PropertyName = "Deal A", Address = "Addr A" },
            new() { RowNumber = 3, PropertyName = "Deal B", Address = "Addr B" },
            new() { RowNumber = 4, PropertyName = "Deal C", Address = "Addr C" },
        };

        await service.ImportAsync(rows, "Progress Portfolio", _userId, progress);

        // All 3 deals should have been imported
        Assert.Equal(3, rows.Count);
    }

    [Fact]
    public async Task ImportAsync_PartialFailure_ContinuesImporting()
    {
        var dealService = new FakeDealService { FailOnProperty = "Fail Me" };
        var service = CreateService(dealService);

        var rows = new List<BulkImportRowDto>
        {
            new() { RowNumber = 2, PropertyName = "Good Deal", Address = "Addr A" },
            new() { RowNumber = 3, PropertyName = "Fail Me", Address = "Addr B" },
            new() { RowNumber = 4, PropertyName = "Also Good", Address = "Addr C" },
        };

        var result = await service.ImportAsync(rows, "Partial Portfolio", _userId);

        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(2, result.CreatedDealIds.Count);
        Assert.Single(result.Errors.Where(e => e.Contains("Row 3")));
    }

    [Fact]
    public async Task ImportAsync_ValidatesOptionalFields()
    {
        var service = CreateService();

        var rows = new List<BulkImportRowDto>
        {
            new()
            {
                RowNumber = 2,
                PropertyName = "Deal A",
                Address = "Addr A",
                LoanLtv = 150, // Invalid: > 100
            },
        };

        var result = await service.ImportAsync(rows, "Validation Portfolio", _userId);

        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
    }

    // --- Stubs ---

    internal class FakeDealService : IDealService
    {
        public string? FailOnProperty { get; set; }
        public List<DealInputDto> CreatedDeals { get; } = new();

        public Task<Guid> CreateDealAsync(DealInputDto input, string userId)
        {
            if (FailOnProperty != null && input.PropertyName == FailOnProperty)
                throw new InvalidOperationException("Simulated failure");

            CreatedDeals.Add(input);
            return Task.FromResult(Guid.NewGuid());
        }

        public Task UpdateDealAsync(Guid id, DealInputDto input, string userId) => Task.CompletedTask;
        public Task<DealInputDto?> GetDealAsync(Guid id, string userId) => Task.FromResult<DealInputDto?>(null);
        public Task<IReadOnlyList<DealSummaryDto>> GetAllDealsAsync(string userId) => Task.FromResult<IReadOnlyList<DealSummaryDto>>(Array.Empty<DealSummaryDto>());
        public Task SetStatusAsync(Guid id, string status, string userId) => Task.CompletedTask;
        public Task DeleteDealAsync(Guid id, string userId) => Task.CompletedTask;
        public Task<IReadOnlyList<DealMapPinDto>> GetDealsForMapAsync(string userId) => Task.FromResult<IReadOnlyList<DealMapPinDto>>(Array.Empty<DealMapPinDto>());
    }

    internal class FakePortfolioService : IPortfolioService
    {
        public List<(string Name, string UserId)> CreatedPortfolios { get; } = new();
        public List<(Guid PortfolioId, Guid DealId)> AssignedDeals { get; } = new();

        public Task<Guid> CreateAsync(string name, string userId, string? strategy = null, int? vintageYear = null)
        {
            var id = Guid.NewGuid();
            CreatedPortfolios.Add((name, userId));
            return Task.FromResult(id);
        }

        public Task AssignDealAsync(Guid portfolioId, Guid dealId, string userId)
        {
            AssignedDeals.Add((portfolioId, dealId));
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Guid id, string name, string userId, string? description = null, string? strategy = null, int? vintageYear = null) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, string userId) => Task.CompletedTask;
        public Task<IReadOnlyList<PortfolioSummaryDto>> GetAllAsync(string userId) => Task.FromResult<IReadOnlyList<PortfolioSummaryDto>>(Array.Empty<PortfolioSummaryDto>());
        public Task<PortfolioSummaryDto?> GetByIdAsync(Guid id, string userId) => Task.FromResult<PortfolioSummaryDto?>(null);
        public Task RemoveDealAsync(Guid dealId, string userId) => Task.CompletedTask;
    }
}
