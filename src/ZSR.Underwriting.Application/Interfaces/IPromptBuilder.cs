using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Models;

namespace ZSR.Underwriting.Application.Interfaces;

/// <summary>
/// Builds structured prompts for Claude AI prose generation.
/// </summary>
public interface IPromptBuilder
{
    ClaudeRequest BuildExecutiveSummaryPrompt(ProseGenerationContext context);
    ClaudeRequest BuildMarketContextPrompt(ProseGenerationContext context);
    ClaudeRequest BuildValueCreationPrompt(ProseGenerationContext context);
    ClaudeRequest BuildRiskAssessmentPrompt(ProseGenerationContext context);
    ClaudeRequest BuildInvestmentDecisionPrompt(ProseGenerationContext context);
    ClaudeRequest BuildPropertyOverviewPrompt(ProseGenerationContext context);
}
