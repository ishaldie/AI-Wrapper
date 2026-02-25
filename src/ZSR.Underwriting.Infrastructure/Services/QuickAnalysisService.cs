using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;

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

    public async Task<QuickAnalysisProgress> StartAnalysisAsync(string searchQuery, string userId, CancellationToken ct = default, IActivityTracker? activityTracker = null)
    {
        var progress = new QuickAnalysisProgress { SearchQuery = searchQuery };

        // Create a minimal deal — the chat page handles all Claude interactions
        progress.SetStepStatus(AnalysisStep.DealCreation, StepStatus.InProgress);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var deal = new Deal(searchQuery, userId);
            deal.PropertyName = searchQuery;
            deal.Address = searchQuery;

            db.Deals.Add(deal);
            await db.SaveChangesAsync(ct);

            progress.DealId = deal.Id;
            progress.SetStepStatus(AnalysisStep.DealCreation, StepStatus.Complete);

            if (activityTracker is not null)
            {
                await activityTracker.TrackEventAsync(ActivityEventType.DealCreated, dealId: deal.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create deal for quick analysis: {Query}", searchQuery);
            progress.SetStepStatus(AnalysisStep.DealCreation, StepStatus.Failed);
            progress.ErrorMessage = "Failed to create deal record.";
        }

        return progress;
    }
}
