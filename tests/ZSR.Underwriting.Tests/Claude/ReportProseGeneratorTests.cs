using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.DTOs.Report;
using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace ZSR.Underwriting.Tests.Claude;

public class ReportProseGeneratorTests
{
    private static ProseGenerationContext CreateContext()
    {
        var deal = new Deal("Test Property");
        deal.PropertyName = "Test Property";
        deal.Address = "123 Main St, Dallas TX";
        deal.UnitCount = 100;
        deal.PurchasePrice = 10_000_000m;

        var calc = new CalculationResult(deal.Id)
        {
            NetOperatingIncome = 700_000m,
            GoingInCapRate = 7.0m,
            DebtServiceCoverageRatio = 1.6m,
            InternalRateOfReturn = 18.0m,
            EquityMultiple = 2.0m
        };

        return new ProseGenerationContext
        {
            Deal = deal,
            Calculations = calc
        };
    }

    // --- Successful generation ---

    [Fact]
    public async Task GenerateAllProseAsync_CallsClaudeForAllSections()
    {
        var callCount = 0;
        var mockClient = new MockClaudeClient(_ =>
        {
            Interlocked.Increment(ref callCount);
            return new ClaudeResponse
            {
                Content = "Generated prose content.",
                Model = "claude-sonnet-4-5-20250929",
                StopReason = "end_turn",
                InputTokens = 100,
                OutputTokens = 50
            };
        });

        var generator = new ReportProseGenerator(
            mockClient, new UnderwritingPromptBuilder(), NullLogger<ReportProseGenerator>.Instance);

        var result = await generator.GenerateAllProseAsync(CreateContext());

        Assert.Equal(6, callCount); // 6 prose sections
    }

    [Fact]
    public async Task GenerateAllProseAsync_PopulatesAllSections()
    {
        var mockClient = new MockClaudeClient(_ => new ClaudeResponse
        {
            Content = "Some prose.",
            Model = "claude-sonnet-4-5-20250929",
            StopReason = "end_turn",
            InputTokens = 100,
            OutputTokens = 50
        });

        var generator = new ReportProseGenerator(
            mockClient, new UnderwritingPromptBuilder(), NullLogger<ReportProseGenerator>.Instance);

        var result = await generator.GenerateAllProseAsync(CreateContext());

        Assert.False(string.IsNullOrWhiteSpace(result.ExecutiveSummaryNarrative));
        Assert.False(string.IsNullOrWhiteSpace(result.MarketContextNarrative));
        Assert.False(string.IsNullOrWhiteSpace(result.ValueCreationNarrative));
        Assert.False(string.IsNullOrWhiteSpace(result.RiskAssessmentNarrative));
        Assert.False(string.IsNullOrWhiteSpace(result.InvestmentThesis));
        Assert.False(string.IsNullOrWhiteSpace(result.PropertyOverviewNarrative));
    }

    [Fact]
    public async Task GenerateAllProseAsync_TracksTokenUsage()
    {
        var mockClient = new MockClaudeClient(_ => new ClaudeResponse
        {
            Content = "Content",
            Model = "claude-sonnet-4-5-20250929",
            StopReason = "end_turn",
            InputTokens = 200,
            OutputTokens = 100
        });

        var generator = new ReportProseGenerator(
            mockClient, new UnderwritingPromptBuilder(), NullLogger<ReportProseGenerator>.Instance);

        var result = await generator.GenerateAllProseAsync(CreateContext());

        Assert.Equal(1200, result.TotalInputTokens); // 200 * 6 sections
        Assert.Equal(600, result.TotalOutputTokens); // 100 * 6 sections
    }

    [Fact]
    public async Task GenerateAllProseAsync_NoFailures_FailedSectionsIsNull()
    {
        var mockClient = new MockClaudeClient(_ => new ClaudeResponse
        {
            Content = "Content",
            Model = "claude-sonnet-4-5-20250929",
            StopReason = "end_turn",
            InputTokens = 10,
            OutputTokens = 5
        });

        var generator = new ReportProseGenerator(
            mockClient, new UnderwritingPromptBuilder(), NullLogger<ReportProseGenerator>.Instance);

        var result = await generator.GenerateAllProseAsync(CreateContext());

        Assert.False(result.HasFailures);
    }

    // --- GO/NO GO decision parsing ---

    [Fact]
    public async Task GenerateAllProseAsync_GoDecision_WhenIrrAndDscrExceedThresholds()
    {
        var ctx = CreateContext();
        // IRR = 18.0 > 15, DSCR = 1.6 > 1.5 → GO
        var mockClient = new MockClaudeClient(_ => new ClaudeResponse
        {
            Content = "Investment thesis here.",
            Model = "claude-sonnet-4-5-20250929",
            StopReason = "end_turn",
            InputTokens = 10,
            OutputTokens = 5
        });

        var generator = new ReportProseGenerator(
            mockClient, new UnderwritingPromptBuilder(), NullLogger<ReportProseGenerator>.Instance);

        var result = await generator.GenerateAllProseAsync(ctx);

        Assert.Equal(InvestmentDecisionType.Go, result.Decision);
    }

    [Fact]
    public async Task GenerateAllProseAsync_NoGoDecision_WhenMetricsBelowThresholds()
    {
        var deal = new Deal("Bad Deal");
        deal.PropertyName = "Bad Deal";
        deal.Address = "999 Fail St";
        deal.UnitCount = 50;
        deal.PurchasePrice = 10_000_000m;

        var calc = new CalculationResult(deal.Id)
        {
            InternalRateOfReturn = 8.0m,  // Below 15%
            DebtServiceCoverageRatio = 1.1m // Below 1.5x
        };

        var ctx = new ProseGenerationContext { Deal = deal, Calculations = calc };

        var mockClient = new MockClaudeClient(_ => new ClaudeResponse
        {
            Content = "Poor investment.",
            Model = "claude-sonnet-4-5-20250929",
            StopReason = "end_turn",
            InputTokens = 10,
            OutputTokens = 5
        });

        var generator = new ReportProseGenerator(
            mockClient, new UnderwritingPromptBuilder(), NullLogger<ReportProseGenerator>.Instance);

        var result = await generator.GenerateAllProseAsync(ctx);

        Assert.Equal(InvestmentDecisionType.NoGo, result.Decision);
    }

    [Fact]
    public async Task GenerateAllProseAsync_ConditionalGoDecision_WhenOneMetricMeets()
    {
        var deal = new Deal("Mixed Deal");
        deal.PropertyName = "Mixed Deal";
        deal.Address = "555 Maybe St";
        deal.UnitCount = 75;
        deal.PurchasePrice = 8_000_000m;

        var calc = new CalculationResult(deal.Id)
        {
            InternalRateOfReturn = 16.0m, // Above 15%
            DebtServiceCoverageRatio = 1.3m // Below 1.5x
        };

        var ctx = new ProseGenerationContext { Deal = deal, Calculations = calc };

        var mockClient = new MockClaudeClient(_ => new ClaudeResponse
        {
            Content = "Conditional investment.",
            Model = "claude-sonnet-4-5-20250929",
            StopReason = "end_turn",
            InputTokens = 10,
            OutputTokens = 5
        });

        var generator = new ReportProseGenerator(
            mockClient, new UnderwritingPromptBuilder(), NullLogger<ReportProseGenerator>.Instance);

        var result = await generator.GenerateAllProseAsync(ctx);

        Assert.Equal(InvestmentDecisionType.ConditionalGo, result.Decision);
    }

    // --- Partial failure handling ---

    [Fact]
    public async Task GenerateAllProseAsync_PartialFailure_RecordsFailedSections()
    {
        var callIndex = 0;
        var mockClient = new MockClaudeClient(_ =>
        {
            var idx = Interlocked.Increment(ref callIndex);
            if (idx == 2) // Second call (Market Context) fails
                throw new HttpRequestException("API error");

            return new ClaudeResponse
            {
                Content = "Content",
                Model = "claude-sonnet-4-5-20250929",
                StopReason = "end_turn",
                InputTokens = 10,
                OutputTokens = 5
            };
        });

        var generator = new ReportProseGenerator(
            mockClient, new UnderwritingPromptBuilder(), NullLogger<ReportProseGenerator>.Instance);

        var result = await generator.GenerateAllProseAsync(CreateContext());

        Assert.True(result.HasFailures);
        Assert.NotNull(result.FailedSections);
        Assert.Single(result.FailedSections!);
    }

    [Fact]
    public async Task GenerateAllProseAsync_PartialFailure_OtherSectionsStillPopulated()
    {
        var callIndex = 0;
        var mockClient = new MockClaudeClient(_ =>
        {
            var idx = Interlocked.Increment(ref callIndex);
            if (idx == 2) // Market Context fails
                throw new HttpRequestException("API error");

            return new ClaudeResponse
            {
                Content = "Good content",
                Model = "claude-sonnet-4-5-20250929",
                StopReason = "end_turn",
                InputTokens = 10,
                OutputTokens = 5
            };
        });

        var generator = new ReportProseGenerator(
            mockClient, new UnderwritingPromptBuilder(), NullLogger<ReportProseGenerator>.Instance);

        var result = await generator.GenerateAllProseAsync(CreateContext());

        // Executive Summary (call 1) should succeed
        Assert.False(string.IsNullOrWhiteSpace(result.ExecutiveSummaryNarrative));
        // Market Context (call 2) should have fallback
        // Other sections should succeed
        Assert.False(string.IsNullOrWhiteSpace(result.PropertyOverviewNarrative));
    }

    // --- No calculations available ---

    [Fact]
    public async Task GenerateAllProseAsync_NoCalculations_DefaultsToConditionalGo()
    {
        var deal = new Deal("NoCalc Deal");
        deal.PropertyName = "NoCalc Deal";
        deal.Address = "1 Test Ln";
        deal.UnitCount = 20;
        deal.PurchasePrice = 2_000_000m;

        var ctx = new ProseGenerationContext { Deal = deal };

        var mockClient = new MockClaudeClient(_ => new ClaudeResponse
        {
            Content = "Content",
            Model = "claude-sonnet-4-5-20250929",
            StopReason = "end_turn",
            InputTokens = 10,
            OutputTokens = 5
        });

        var generator = new ReportProseGenerator(
            mockClient, new UnderwritingPromptBuilder(), NullLogger<ReportProseGenerator>.Instance);

        var result = await generator.GenerateAllProseAsync(ctx);

        Assert.Equal(InvestmentDecisionType.ConditionalGo, result.Decision);
    }
}

/// <summary>
/// Mock IClaudeClient that uses a delegate to produce responses.
/// </summary>
internal class MockClaudeClient : IClaudeClient
{
    private readonly Func<ClaudeRequest, ClaudeResponse> _handler;

    public MockClaudeClient(Func<ClaudeRequest, ClaudeResponse> handler)
    {
        _handler = handler;
    }

    public Task<ClaudeResponse> SendMessageAsync(ClaudeRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(_handler(request));
    }
}
