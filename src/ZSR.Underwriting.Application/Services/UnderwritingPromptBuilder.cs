using System.Globalization;
using System.Text;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Models;

namespace ZSR.Underwriting.Application.Services;

public class UnderwritingPromptBuilder : IPromptBuilder
{
    private const string SystemRole =
        "You are a senior multifamily real estate underwriting analyst at ZSR Ventures. " +
        "You produce institutional-quality prose for underwriting reports. " +
        "Be precise with numbers, concise in language, and analytical in tone. " +
        "Do not use markdown headers or bullet points unless explicitly requested.";

    public ClaudeRequest BuildExecutiveSummaryPrompt(ProseGenerationContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Write an executive summary for the following multifamily acquisition opportunity.");
        sb.AppendLine();
        AppendPropertyHeader(sb, context);
        AppendFinancialMetrics(sb, context);
        sb.AppendLine();
        sb.AppendLine("The executive summary should include:");
        sb.AppendLine("1. A one-line investment thesis");
        sb.AppendLine("2. A 2-3 paragraph narrative covering the deal highlights, financial profile, and market positioning");
        sb.AppendLine("3. Top 3 key highlights (strengths)");
        sb.AppendLine("4. Top 3 key risks");

        return new ClaudeRequest
        {
            SystemPrompt = SystemRole + " Focus on the executive summary for this underwriting report.",
            UserMessage = sb.ToString(),
            MaxTokens = 2048
        };
    }

    public ClaudeRequest BuildMarketContextPrompt(ProseGenerationContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Write a market context analysis for the following multifamily acquisition.");
        sb.AppendLine();
        AppendPropertyHeader(sb, context);

        if (context.MarketContext is { } mc)
        {
            if (mc.MajorEmployers.Count > 0)
            {
                sb.AppendLine("## Major Employers");
                foreach (var emp in mc.MajorEmployers)
                    sb.AppendLine($"- {emp.Name}: {emp.Description}");
            }
            if (mc.ConstructionPipeline.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("## Construction Pipeline");
                foreach (var item in mc.ConstructionPipeline)
                    sb.AppendLine($"- {item.Name}: {item.Description}");
            }
            if (mc.EconomicDrivers.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("## Economic Drivers");
                foreach (var driver in mc.EconomicDrivers)
                    sb.AppendLine($"- {driver.Name}: {driver.Description}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Write a 2-3 paragraph market context covering supply-demand dynamics, economic drivers, and rent growth outlook.");

        return new ClaudeRequest
        {
            SystemPrompt = SystemRole + " Focus on market context and economic analysis.",
            UserMessage = sb.ToString(),
            MaxTokens = 1536
        };
    }

    public ClaudeRequest BuildValueCreationPrompt(ProseGenerationContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Write a value creation strategy for the following multifamily acquisition.");
        sb.AppendLine();
        AppendPropertyHeader(sb, context);

        sb.AppendLine("## Value-Add Plans");
        if (!string.IsNullOrWhiteSpace(context.Deal.ValueAddPlans))
            sb.AppendLine($"- Plans: {context.Deal.ValueAddPlans}");
        else
            sb.AppendLine("- No specific value-add plans provided");

        if (context.Deal.CapexBudget.HasValue)
            sb.AppendLine($"- Capital Budget: {FormatCurrency(context.Deal.CapexBudget.Value)}");

        if (context.Calculations is { } calc)
        {
            if (calc.GoingInCapRate.HasValue)
                sb.AppendLine($"- Going-In Cap Rate: {calc.GoingInCapRate.Value:N1}%");
            if (calc.ExitCapRate.HasValue)
                sb.AppendLine($"- Exit Cap Rate: {calc.ExitCapRate.Value:N1}%");
        }

        sb.AppendLine();
        sb.AppendLine("Write a value creation strategy including execution timeline, capital requirements, and expected return impact.");

        return new ClaudeRequest
        {
            SystemPrompt = SystemRole + " Focus on value creation strategy and execution planning.",
            UserMessage = sb.ToString(),
            MaxTokens = 1536
        };
    }

    public ClaudeRequest BuildRiskAssessmentPrompt(ProseGenerationContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analyze the risks for the following multifamily acquisition.");
        sb.AppendLine();
        AppendPropertyHeader(sb, context);
        AppendFinancialMetrics(sb, context);

        sb.AppendLine();
        sb.AppendLine("Identify and analyze the key risks. For each risk provide:");
        sb.AppendLine("- Category (Market, Financial, Operational, Regulatory)");
        sb.AppendLine("- Description of the risk");
        sb.AppendLine("- Severity level (Low, Medium, High)");
        sb.AppendLine("- Mitigation strategy");

        return new ClaudeRequest
        {
            SystemPrompt = SystemRole + " Focus on risk assessment. Identify risks with specific severity levels (Low, Medium, High) and mitigation strategies.",
            UserMessage = sb.ToString(),
            MaxTokens = 2048
        };
    }

    public ClaudeRequest BuildInvestmentDecisionPrompt(ProseGenerationContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Make an investment decision for the following multifamily acquisition.");
        sb.AppendLine();
        AppendPropertyHeader(sb, context);
        AppendFinancialMetrics(sb, context);

        sb.AppendLine("## Protocol Decision Thresholds");
        sb.AppendLine("- GO: IRR > 15% AND DSCR > 1.5x");
        sb.AppendLine("- CONDITIONAL GO: Meets one threshold but not both, or close to both");
        sb.AppendLine("- NO GO: Fails both thresholds significantly");

        if (context.Calculations is { } calc)
        {
            sb.AppendLine();
            sb.AppendLine("## Actual Metrics vs Thresholds");
            sb.AppendLine($"- IRR: {FormatDecimalOrNa(calc.InternalRateOfReturn)}% (threshold: 15%)");
            sb.AppendLine($"- DSCR: {FormatDecimalOrNa(calc.DebtServiceCoverageRatio)}x (threshold: 1.5x)");
        }

        sb.AppendLine();
        sb.AppendLine("Provide:");
        sb.AppendLine("1. Investment decision: GO, CONDITIONAL GO, or NO GO");
        sb.AppendLine("2. Investment thesis (2-3 sentences)");
        sb.AppendLine("3. Conditions precedent (if CONDITIONAL GO)");
        sb.AppendLine("4. Recommended next steps");

        return new ClaudeRequest
        {
            SystemPrompt = SystemRole + " Make a GO, CONDITIONAL GO, or NO GO investment decision based on the ZSR Ventures underwriting protocol.",
            UserMessage = sb.ToString(),
            MaxTokens = 1536
        };
    }

    public ClaudeRequest BuildPropertyOverviewPrompt(ProseGenerationContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Write a property overview paragraph for the following multifamily asset.");
        sb.AppendLine();
        AppendPropertyHeader(sb, context);

        sb.AppendLine();
        sb.AppendLine("Write a concise property overview paragraph (3-5 sentences) describing the physical asset, location, and market positioning.");

        return new ClaudeRequest
        {
            SystemPrompt = SystemRole + " Write a concise property overview for the underwriting report.",
            UserMessage = sb.ToString(),
            MaxTokens = 512
        };
    }

    // --- Helper methods ---

    private static void AppendPropertyHeader(StringBuilder sb, ProseGenerationContext context)
    {
        var deal = context.Deal;
        sb.AppendLine("## Property Information");
        sb.AppendLine($"- Property: {deal.PropertyName}");
        sb.AppendLine($"- Address: {deal.Address}");
        sb.AppendLine($"- Units: {deal.UnitCount:N0}");
        sb.AppendLine($"- Purchase Price: {FormatCurrency(deal.PurchasePrice)}");
        if (deal.PurchasePrice > 0 && deal.UnitCount > 0)
            sb.AppendLine($"- Price/Unit: {FormatCurrency(deal.PurchasePrice / deal.UnitCount)}");
        sb.AppendLine();
    }

    private static void AppendFinancialMetrics(StringBuilder sb, ProseGenerationContext context)
    {
        if (context.Calculations is not { } calc) return;

        sb.AppendLine("## Financial Metrics");
        AppendIfPresent(sb, "NOI", calc.NetOperatingIncome, currency: true);
        AppendIfPresent(sb, "Going-In Cap Rate", calc.GoingInCapRate, "%");
        AppendIfPresent(sb, "DSCR", calc.DebtServiceCoverageRatio, "x");
        AppendIfPresent(sb, "Cash-on-Cash Return", calc.CashOnCashReturn, "%");
        AppendIfPresent(sb, "IRR", calc.InternalRateOfReturn, "%");
        AppendIfPresent(sb, "Equity Multiple", calc.EquityMultiple, "x");
        AppendIfPresent(sb, "Exit Value", calc.ExitValue, currency: true);
        AppendIfPresent(sb, "Total Profit", calc.TotalProfit, currency: true);
        AppendIfPresent(sb, "Loan Amount", calc.LoanAmount, currency: true);
        AppendIfPresent(sb, "Annual Debt Service", calc.AnnualDebtService, currency: true);
        sb.AppendLine();
    }

    private static void AppendIfPresent(StringBuilder sb, string label, decimal? value, string suffix = "", bool currency = false, bool formatInt = false)
    {
        if (!value.HasValue) return;
        if (currency)
            sb.AppendLine($"- {label}: {FormatCurrency(value.Value)}");
        else if (formatInt)
            sb.AppendLine($"- {label}: {value.Value:N0}{suffix}");
        else
            sb.AppendLine($"- {label}: {value.Value:N2}{suffix}");
    }

    private static void AppendIfPresent(StringBuilder sb, string label, int? value, string suffix = "", bool formatInt = true)
    {
        if (!value.HasValue) return;
        sb.AppendLine($"- {label}: {value.Value:N0}{suffix}");
    }

    private static string FormatCurrency(decimal value) =>
        value.ToString("C0", CultureInfo.GetCultureInfo("en-US"));

    private static string FormatDecimalOrNa(decimal? value) =>
        value.HasValue ? value.Value.ToString("N2") : "N/A";
}
