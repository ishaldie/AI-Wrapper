using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.ValueObjects;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Mapping;

namespace ZSR.Underwriting.Infrastructure.Services;

public class QuickAnalysisService : IQuickAnalysisService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<QuickAnalysisService> _logger;

    public QuickAnalysisService(
        IServiceScopeFactory scopeFactory,
        ILogger<QuickAnalysisService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<QuickAnalysisProgress> StartAnalysisAsync(string searchQuery, CancellationToken ct = default)
    {
        var progress = new QuickAnalysisProgress { SearchQuery = searchQuery };

        // Step 1 (sync): Create a minimal deal
        progress.SetStepStatus(AnalysisStep.DealCreation, StepStatus.InProgress);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var deal = new Deal(searchQuery);
            deal.PropertyName = searchQuery;
            deal.Address = searchQuery;

            db.Deals.Add(deal);
            await db.SaveChangesAsync(ct);

            progress.DealId = deal.Id;
            progress.SetStepStatus(AnalysisStep.DealCreation, StepStatus.Complete);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create deal for quick analysis: {Query}", searchQuery);
            progress.SetStepStatus(AnalysisStep.DealCreation, StepStatus.Failed);
            progress.ErrorMessage = "Failed to create deal record.";
            return progress;
        }

        // Register progress for polling from other circuits
        QuickAnalysisTracker.Register(progress.DealId, progress);

        // Step 2+ (background): Run enrichment pipeline
        var dealId = progress.DealId;
        _ = Task.Run(() => RunBackgroundAnalysis(dealId, searchQuery, progress));

        return progress;
    }

    private async Task RunBackgroundAnalysis(Guid dealId, string address, QuickAnalysisProgress progress)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var realAi = scope.ServiceProvider.GetRequiredService<IRealAiClient>();

            // Fan out 5 RealAI calls in parallel
            await RunRealAiSteps(dealId, address, progress, db, realAi);

            // Market context (web search) — skip gracefully if not configured
            await RunMarketContext(dealId, address, progress, scope);

            // Assemble report
            await RunReportAssembly(dealId, progress, scope);

            // AI prose generation — skip gracefully if not configured
            await RunAiProse(dealId, progress, scope);

            // Set deal status to InProgress
            var deal = await db.Deals.FindAsync(dealId);
            if (deal != null)
            {
                deal.UpdateStatus(DealStatus.InProgress);
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background analysis failed for deal {DealId}", dealId);
            progress.ErrorMessage = "Analysis encountered an unexpected error.";
        }
    }

    private async Task RunRealAiSteps(
        Guid dealId, string address, QuickAnalysisProgress progress,
        AppDbContext db, IRealAiClient realAi)
    {
        // Mark all 5 data steps as in-progress
        progress.SetStepStatus(AnalysisStep.PropertyData, StepStatus.InProgress);
        progress.SetStepStatus(AnalysisStep.TenantMetrics, StepStatus.InProgress);
        progress.SetStepStatus(AnalysisStep.MarketData, StepStatus.InProgress);
        progress.SetStepStatus(AnalysisStep.SalesComps, StepStatus.InProgress);
        progress.SetStepStatus(AnalysisStep.TimeSeries, StepStatus.InProgress);

        // Run all 5 in parallel, each completing independently
        var propertyTask = SafeCall(
            () => realAi.GetPropertyDataAsync(address),
            progress, AnalysisStep.PropertyData);

        var tenantTask = SafeCall(
            () => realAi.GetTenantMetricsAsync(address),
            progress, AnalysisStep.TenantMetrics);

        var marketTask = SafeCall(
            () => realAi.GetMarketDataAsync(address),
            progress, AnalysisStep.MarketData);

        var compsTask = SafeCall(
            async () => (IReadOnlyList<SalesComp>?)await realAi.GetSalesCompsAsync(address),
            progress, AnalysisStep.SalesComps);

        var timeSeriesTask = SafeCall(
            () => realAi.GetTimeSeriesAsync(address),
            progress, AnalysisStep.TimeSeries);

        await Task.WhenAll(propertyTask, tenantTask, marketTask, compsTask, timeSeriesTask);

        var property = propertyTask.Result;
        var tenant = tenantTask.Result;
        var market = marketTask.Result;
        var comps = compsTask.Result;
        var timeSeries = timeSeriesTask.Result;

        // Save RealAI data to deal
        try
        {
            var realAiData = RealAiDataMapper.Map(dealId, property, tenant, market, comps, timeSeries);
            db.Set<RealAiData>().Add(realAiData);

            // Enrich deal with property data if available
            var deal = await db.Deals.FindAsync(dealId);
            if (deal != null && property != null)
            {
                if (property.InPlaceRent.HasValue)
                    deal.RentRollSummary = property.InPlaceRent;
                if (property.Occupancy.HasValue)
                    deal.TargetOccupancy = property.Occupancy;
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist RealAI data for deal {DealId}", dealId);
        }
    }

    private async Task RunMarketContext(
        Guid dealId, string address, QuickAnalysisProgress progress, IServiceScope scope)
    {
        progress.SetStepStatus(AnalysisStep.MarketContext, StepStatus.InProgress);
        try
        {
            var marketDataService = scope.ServiceProvider.GetService<MarketDataService>();
            if (marketDataService == null)
            {
                _logger.LogInformation("MarketDataService not registered, skipping market context");
                progress.SetStepStatus(AnalysisStep.MarketContext, StepStatus.Complete);
                return;
            }

            // Extract city/state from address (simple heuristic)
            var (city, state) = ParseCityState(address);
            await marketDataService.GetMarketContextForDealAsync(dealId, city, state);
            progress.SetStepStatus(AnalysisStep.MarketContext, StepStatus.Complete);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Market context lookup failed for deal {DealId}", dealId);
            progress.SetStepStatus(AnalysisStep.MarketContext, StepStatus.Failed);
        }
    }

    private async Task RunReportAssembly(
        Guid dealId, QuickAnalysisProgress progress, IServiceScope scope)
    {
        progress.SetStepStatus(AnalysisStep.ReportAssembly, StepStatus.InProgress);
        try
        {
            var assembler = scope.ServiceProvider.GetRequiredService<IReportAssembler>();
            await assembler.AssembleReportAsync(dealId);
            progress.SetStepStatus(AnalysisStep.ReportAssembly, StepStatus.Complete);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Report assembly failed for deal {DealId}", dealId);
            progress.SetStepStatus(AnalysisStep.ReportAssembly, StepStatus.Failed);
        }
    }

    private async Task RunAiProse(
        Guid dealId, QuickAnalysisProgress progress, IServiceScope scope)
    {
        progress.SetStepStatus(AnalysisStep.AiProse, StepStatus.InProgress);
        try
        {
            var proseGenerator = scope.ServiceProvider.GetService<IReportProseGenerator>();
            if (proseGenerator == null)
            {
                _logger.LogInformation("IReportProseGenerator not registered, skipping AI prose");
                progress.SetStepStatus(AnalysisStep.AiProse, StepStatus.Complete);
                return;
            }

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var deal = await db.Deals
                .Include(d => d.CalculationResult)
                .Include(d => d.RealAiData)
                .FirstOrDefaultAsync(d => d.Id == dealId);

            if (deal == null)
            {
                progress.SetStepStatus(AnalysisStep.AiProse, StepStatus.Failed);
                return;
            }

            var context = new ProseGenerationContext
            {
                Deal = deal,
                Calculations = deal.CalculationResult,
                RealAiData = deal.RealAiData
            };

            await proseGenerator.GenerateAllProseAsync(context);
            progress.SetStepStatus(AnalysisStep.AiProse, StepStatus.Complete);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI prose generation failed for deal {DealId}", dealId);
            progress.SetStepStatus(AnalysisStep.AiProse, StepStatus.Failed);
        }
    }

    private async Task<T?> SafeCall<T>(
        Func<Task<T?>> call,
        QuickAnalysisProgress progress,
        AnalysisStep step)
    {
        try
        {
            var result = await call();
            progress.SetStepStatus(step, StepStatus.Complete);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RealAI step {Step} failed", step);
            progress.SetStepStatus(step, StepStatus.Failed);
            return default;
        }
    }

    private static (string city, string state) ParseCityState(string address)
    {
        // Try to extract "City, ST" from an address like "123 Main St, Dallas, TX 75201"
        var parts = address.Split(',');
        if (parts.Length >= 2)
        {
            var city = parts[^2].Trim();
            var stateZip = parts[^1].Trim().Split(' ');
            var state = stateZip.Length > 0 ? stateZip[0].Trim() : "";
            return (city, state);
        }

        return (address.Trim(), "");
    }
}
