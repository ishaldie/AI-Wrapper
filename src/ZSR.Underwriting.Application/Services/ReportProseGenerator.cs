using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.DTOs.Report;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Exceptions;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.Models;

namespace ZSR.Underwriting.Application.Services;

public class ReportProseGenerator : IReportProseGenerator
{
    private readonly IClaudeClient _claude;
    private readonly IPromptBuilder _promptBuilder;
    private readonly ILogger<ReportProseGenerator> _logger;

    public ReportProseGenerator(
        IClaudeClient claude,
        IPromptBuilder promptBuilder,
        ILogger<ReportProseGenerator> logger)
    {
        _claude = claude;
        _promptBuilder = promptBuilder;
        _logger = logger;
    }

    public async Task<GeneratedProse> GenerateAllProseAsync(
        ProseGenerationContext context, CancellationToken ct = default)
    {
        var failedSections = new List<string>();
        var totalInput = 0;
        var totalOutput = 0;

        var userId = context.UserId;

        // Generate all 6 sections sequentially (to stay within rate limits)
        var execSummary = await GenerateSectionAsync(
            "Executive Summary",
            () => _promptBuilder.BuildExecutiveSummaryPrompt(context),
            userId, failedSections, ct);
        totalInput += execSummary.InputTokens;
        totalOutput += execSummary.OutputTokens;

        var marketContext = await GenerateSectionAsync(
            "Market Context",
            () => _promptBuilder.BuildMarketContextPrompt(context),
            userId, failedSections, ct);
        totalInput += marketContext.InputTokens;
        totalOutput += marketContext.OutputTokens;

        var valueCreation = await GenerateSectionAsync(
            "Value Creation",
            () => _promptBuilder.BuildValueCreationPrompt(context),
            userId, failedSections, ct);
        totalInput += valueCreation.InputTokens;
        totalOutput += valueCreation.OutputTokens;

        var riskAssessment = await GenerateSectionAsync(
            "Risk Assessment",
            () => _promptBuilder.BuildRiskAssessmentPrompt(context),
            userId, failedSections, ct);
        totalInput += riskAssessment.InputTokens;
        totalOutput += riskAssessment.OutputTokens;

        var investmentDecision = await GenerateSectionAsync(
            "Investment Decision",
            () => _promptBuilder.BuildInvestmentDecisionPrompt(context),
            userId, failedSections, ct);
        totalInput += investmentDecision.InputTokens;
        totalOutput += investmentDecision.OutputTokens;

        var propertyOverview = await GenerateSectionAsync(
            "Property Overview",
            () => _promptBuilder.BuildPropertyOverviewPrompt(context),
            userId, failedSections, ct);
        totalInput += propertyOverview.InputTokens;
        totalOutput += propertyOverview.OutputTokens;

        var decision = DetermineDecision(context);

        _logger.LogInformation(
            "Prose generation complete: total_input_tokens={InputTokens}, total_output_tokens={OutputTokens}, failed_sections={FailedCount}",
            totalInput, totalOutput, failedSections.Count);

        return new GeneratedProse
        {
            ExecutiveSummaryNarrative = execSummary.Content,
            KeyHighlights = [], // Parsed from AI response in future enhancement
            KeyRisks = [],

            MarketContextNarrative = marketContext.Content,

            ValueCreationNarrative = valueCreation.Content,

            RiskAssessmentNarrative = riskAssessment.Content,
            Risks = [],

            Decision = decision,
            InvestmentThesis = investmentDecision.Content,
            Conditions = [],
            NextSteps = [],

            PropertyOverviewNarrative = propertyOverview.Content,

            TotalInputTokens = totalInput,
            TotalOutputTokens = totalOutput,
            FailedSections = failedSections.Count > 0 ? failedSections : null
        };
    }

    private async Task<ClaudeResponse> GenerateSectionAsync(
        string sectionName,
        Func<ClaudeRequest> buildPrompt,
        string? userId,
        List<string> failedSections,
        CancellationToken ct)
    {
        try
        {
            var built = buildPrompt();
            var request = new ClaudeRequest
            {
                SystemPrompt = built.SystemPrompt,
                UserMessage = built.UserMessage,
                MaxTokens = built.MaxTokens,
                ConversationHistory = built.ConversationHistory,
                UserId = userId
            };
            _logger.LogInformation("Generating prose section: {Section}", sectionName);
            var response = await _claude.SendMessageAsync(request, ct);
            _logger.LogInformation("Section {Section} generated: {OutputTokens} tokens",
                sectionName, response.OutputTokens);
            return response;
        }
        catch (ClaudeRateLimitException ex)
        {
            _logger.LogWarning("Rate limited generating section: {Section}. Retry after: {RetryAfter}s",
                sectionName, ex.RetryAfterSeconds);
            failedSections.Add($"{sectionName} (rate limited)");
            return new ClaudeResponse
            {
                Content = $"[{sectionName} skipped — API rate limit reached]",
                InputTokens = 0,
                OutputTokens = 0
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to generate section: {Section}", sectionName);
            failedSections.Add(sectionName);
            return new ClaudeResponse
            {
                Content = $"[{sectionName} generation failed — will retry on next report run]",
                InputTokens = 0,
                OutputTokens = 0
            };
        }
    }

    private static InvestmentDecisionType DetermineDecision(ProseGenerationContext context)
    {
        if (context.Calculations is not { } calc)
            return InvestmentDecisionType.ConditionalGo;

        var irr = calc.InternalRateOfReturn;
        var dscr = calc.DebtServiceCoverageRatio;

        if (!irr.HasValue || !dscr.HasValue)
            return InvestmentDecisionType.ConditionalGo;

        var irrMeetsThreshold = irr.Value >= 15m;
        var dscrMeetsThreshold = dscr.Value >= 1.5m;

        return (irrMeetsThreshold, dscrMeetsThreshold) switch
        {
            (true, true) => InvestmentDecisionType.Go,
            (false, false) => InvestmentDecisionType.NoGo,
            _ => InvestmentDecisionType.ConditionalGo
        };
    }
}
