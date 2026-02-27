using System.Globalization;
using System.Text;
using System.Text.Json;
using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Models;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Application.Services;

public class UnderwritingPromptBuilder : IPromptBuilder
{
    private const string BaseSystemSuffix =
        "You produce institutional-quality prose for underwriting reports. " +
        "Be precise with numbers, concise in language, and analytical in tone. " +
        "Do not use markdown headers or bullet points unless explicitly requested.";

    private const string ConcisenessDirective =
        "Keep each point to 2-3 sentences. No preamble or recap. Start directly with analysis.";

    private static string BuildSystemRole(PropertyType type, string sectionFocus)
    {
        var intro = type switch
        {
            PropertyType.Bridge =>
                "You are a bridge loan underwriting analyst at ZSR Ventures specializing in short-term " +
                "transitional financing for value-add multifamily and commercial assets. You understand " +
                "bridge-to-permanent strategies, renovation scope analysis, lease-up projections, and " +
                "interest reserve structuring. ",
            PropertyType.Hospitality =>
                "You are a hospitality real estate underwriting analyst at ZSR Ventures specializing in hotel " +
                "and lodging acquisitions. You understand RevPAR dynamics, ADR trends, occupancy seasonality, " +
                "franchise vs independent operations, PIP requirements, and room-based revenue models. ",
            PropertyType.Commercial =>
                "You are a commercial real estate underwriting analyst at ZSR Ventures specializing in office, " +
                "retail, and mixed-use properties. You understand tenant credit analysis, lease rollover risk, " +
                "NRA-based rent structures, CAM reconciliation, and net-lease vs gross-lease economics. ",
            PropertyType.LIHTC =>
                "You are an affordable housing underwriting analyst at ZSR Ventures specializing in LIHTC " +
                "(Low-Income Housing Tax Credit) properties. You understand tax credit compliance, AMI rent " +
                "limits, LURA restrictions, qualified allocation plans, and regulatory agreement structures. ",
            _ when ProtocolDefaults.IsSeniorHousing(type) =>
                "You are a senior housing underwriting analyst at ZSR Ventures specializing in assisted living, " +
                "skilled nursing, memory care, and CCRC facilities. You understand payer mix dynamics (private pay, " +
                "Medicaid, Medicare), staffing-driven cost structures, regulatory compliance (CMS star ratings, " +
                "deficiency citations), and bed-based revenue models. ",
            _ =>
                "You are a senior multifamily real estate underwriting analyst at ZSR Ventures. ",
        };

        return intro + BaseSystemSuffix + sectionFocus;
    }

    private static string GetAssetTypeLabel(PropertyType type) => type switch
    {
        PropertyType.AssistedLiving => "assisted living",
        PropertyType.SkilledNursing => "skilled nursing facility",
        PropertyType.MemoryCare => "memory care",
        PropertyType.CCRC => "continuing care retirement community (CCRC)",
        PropertyType.Bridge => "bridge loan",
        PropertyType.Hospitality => "hotel/hospitality",
        PropertyType.Commercial => "commercial",
        PropertyType.LIHTC => "affordable housing (LIHTC)",
        PropertyType.BoardAndCare => "board and care",
        PropertyType.IndependentLiving => "independent living",
        PropertyType.SeniorApartment => "senior apartment",
        _ => "multifamily"
    };

    public ClaudeRequest BuildExecutiveSummaryPrompt(ProseGenerationContext context)
    {
        var assetType = GetAssetTypeLabel(context.Deal.PropertyType);
        var sb = new StringBuilder();
        sb.AppendLine($"Write an executive summary for the following {assetType} acquisition opportunity.");
        sb.AppendLine();
        AppendPropertyHeader(sb, context);
        AppendFannieProductHeader(sb, context);
        AppendFreddieProductHeader(sb, context);
        AppendSeniorHousingMetrics(sb, context);
        AppendFinancialMetrics(sb, context);
        AppendFannieComplianceSummaryLine(sb, context);
        AppendFreddieComplianceSummaryLine(sb, context);
        sb.AppendLine();
        sb.AppendLine("The executive summary should include:");
        sb.AppendLine("1. A one-line investment thesis");
        sb.AppendLine("2. A 2-3 paragraph narrative covering the deal highlights, financial profile, and market positioning");
        sb.AppendLine("3. Top 3 key highlights (strengths)");
        sb.AppendLine("4. Top 3 key risks");
        sb.AppendLine();
        sb.AppendLine(ConcisenessDirective);

        return new ClaudeRequest
        {
            SystemPrompt = BuildSystemRole(context.Deal.PropertyType, " Focus on the executive summary for this underwriting report."),
            UserMessage = sb.ToString(),
            MaxTokens = 1024
        };
    }

    public ClaudeRequest BuildMarketContextPrompt(ProseGenerationContext context)
    {
        var assetType = GetAssetTypeLabel(context.Deal.PropertyType);
        var sb = new StringBuilder();
        sb.AppendLine($"Write a market context analysis for the following {assetType} acquisition.");
        sb.AppendLine();
        AppendCompactPropertyLine(sb, context);
        AppendSeniorHousingMetrics(sb, context);

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
        if (ProtocolDefaults.IsSeniorHousing(context.Deal.PropertyType))
            sb.AppendLine("Write a 2-3 paragraph market context covering senior housing supply-demand dynamics, demographic trends (aging population), regulatory environment, and rate growth outlook.");
        else
            sb.AppendLine("Write a 2-3 paragraph market context covering supply-demand dynamics, economic drivers, and rent growth outlook.");
        sb.AppendLine();
        sb.AppendLine(ConcisenessDirective);

        return new ClaudeRequest
        {
            SystemPrompt = BuildSystemRole(context.Deal.PropertyType, " Focus on market context and economic analysis."),
            UserMessage = sb.ToString(),
            MaxTokens = 1024
        };
    }

    public ClaudeRequest BuildValueCreationPrompt(ProseGenerationContext context)
    {
        var assetType = GetAssetTypeLabel(context.Deal.PropertyType);
        var sb = new StringBuilder();
        sb.AppendLine($"Write a value creation strategy for the following {assetType} acquisition.");
        sb.AppendLine();
        AppendCompactPropertyLine(sb, context);

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
        sb.AppendLine();
        sb.AppendLine(ConcisenessDirective);

        return new ClaudeRequest
        {
            SystemPrompt = BuildSystemRole(context.Deal.PropertyType, " Focus on value creation strategy and execution planning."),
            UserMessage = sb.ToString(),
            MaxTokens = 1024
        };
    }

    public ClaudeRequest BuildRiskAssessmentPrompt(ProseGenerationContext context)
    {
        var assetType = GetAssetTypeLabel(context.Deal.PropertyType);
        var isFannie = context.Deal.FannieProductType.HasValue;
        var isFreddie = context.Deal.FreddieProductType.HasValue;
        var sb = new StringBuilder();
        sb.AppendLine($"Analyze the risks for the following {assetType} acquisition.");
        sb.AppendLine();
        AppendPropertyHeader(sb, context);
        AppendFannieProductHeader(sb, context);
        AppendFreddieProductHeader(sb, context);
        AppendSeniorHousingMetrics(sb, context, includeCms: true);
        AppendFinancialMetrics(sb, context);
        AppendFannieComplianceSection(sb, context);
        AppendFreddieComplianceSection(sb, context);

        sb.AppendLine();
        sb.AppendLine("Identify and analyze the key risks. For each risk provide:");
        sb.AppendLine("- Category (Market, Financial, Operational, Regulatory)");
        sb.AppendLine("- Description of the risk");
        sb.AppendLine("- Severity level (Low, Medium, High)");
        sb.AppendLine("- Mitigation strategy");
        sb.AppendLine();
        sb.AppendLine(ConcisenessDirective);

        var systemSuffix = isFannie
            ? " Focus on risk assessment. Identify risks with specific severity levels (Low, Medium, High) and mitigation strategies. Include Fannie Mae compliance risks and product-specific regulatory requirements."
            : isFreddie
                ? " Focus on risk assessment. Identify risks with specific severity levels (Low, Medium, High) and mitigation strategies. Include Freddie Mac compliance risks and product-specific regulatory requirements."
                : " Focus on risk assessment. Identify risks with specific severity levels (Low, Medium, High) and mitigation strategies.";

        return new ClaudeRequest
        {
            SystemPrompt = BuildSystemRole(context.Deal.PropertyType, systemSuffix),
            UserMessage = sb.ToString(),
            MaxTokens = 1536
        };
    }

    public ClaudeRequest BuildInvestmentDecisionPrompt(ProseGenerationContext context)
    {
        var assetType = GetAssetTypeLabel(context.Deal.PropertyType);
        var isFannie = context.Deal.FannieProductType.HasValue;
        var isFreddie = context.Deal.FreddieProductType.HasValue;
        var sb = new StringBuilder();
        sb.AppendLine($"Make an investment decision for the following {assetType} acquisition.");
        sb.AppendLine();
        AppendCompactPropertyLine(sb, context);
        AppendFannieProductHeader(sb, context);
        AppendFreddieProductHeader(sb, context);
        AppendFinancialMetrics(sb, context);

        if (isFannie)
        {
            var compliance = DeserializeFannieCompliance(context);
            var profile = FannieProductProfiles.TryGet(context.Deal.FannieProductType);

            sb.AppendLine("## Fannie Mae Product Decision Thresholds");
            if (profile != null)
            {
                sb.AppendLine($"- Product Minimum DSCR: {profile.MinDscr:F2}x");
                sb.AppendLine($"- Product Maximum LTV: {profile.MaxLtvPercent:F0}%");
                sb.AppendLine($"- Maximum Amortization: {profile.MaxAmortizationYears} years");
            }

            if (compliance != null)
            {
                sb.AppendLine($"- Overall Fannie Mae Compliance: {(compliance.OverallPass ? "PASS" : "FAIL")}");
            }

            sb.AppendLine();
            sb.AppendLine("- GO: DSCR meets product minimum AND Fannie Mae compliance PASS AND IRR > 15%");
            sb.AppendLine("- CONDITIONAL GO: Meets most criteria but has one or more compliance warnings");
            sb.AppendLine("- NO GO: Fails Fannie Mae compliance or significantly misses product thresholds");

            if (context.Calculations is { } calc)
            {
                sb.AppendLine();
                sb.AppendLine("## Actual Metrics vs Thresholds");
                sb.AppendLine($"- IRR: {FormatDecimalOrNa(calc.InternalRateOfReturn)}% (threshold: 15%)");
                sb.AppendLine($"- DSCR: {FormatDecimalOrNa(calc.DebtServiceCoverageRatio)}x (product min: {profile?.MinDscr.ToString("F2") ?? "N/A"}x)");
            }

            AppendFannieComplianceSection(sb, context);
        }
        else if (isFreddie)
        {
            var compliance = DeserializeFreddieCompliance(context);
            var profile = FreddieProductProfiles.TryGet(context.Deal.FreddieProductType);

            sb.AppendLine("## Freddie Mac Product Decision Thresholds");
            if (profile != null)
            {
                sb.AppendLine($"- Product Minimum DSCR: {profile.MinDscr:F2}x");
                sb.AppendLine($"- Product Maximum LTV: {profile.MaxLtvPercent:F0}%");
                sb.AppendLine($"- Maximum Amortization: {profile.MaxAmortizationYears} years");
            }

            if (compliance != null)
            {
                sb.AppendLine($"- Overall Freddie Mac Compliance: {(compliance.OverallPass ? "PASS" : "FAIL")}");
            }

            sb.AppendLine();
            sb.AppendLine("- GO: DSCR meets product minimum AND Freddie Mac compliance PASS AND IRR > 15%");
            sb.AppendLine("- CONDITIONAL GO: Meets most criteria but has one or more compliance warnings");
            sb.AppendLine("- NO GO: Fails Freddie Mac compliance or significantly misses product thresholds");

            if (context.Calculations is { } calc)
            {
                sb.AppendLine();
                sb.AppendLine("## Actual Metrics vs Thresholds");
                sb.AppendLine($"- IRR: {FormatDecimalOrNa(calc.InternalRateOfReturn)}% (threshold: 15%)");
                sb.AppendLine($"- DSCR: {FormatDecimalOrNa(calc.DebtServiceCoverageRatio)}x (product min: {profile?.MinDscr.ToString("F2") ?? "N/A"}x)");
            }

            AppendFreddieComplianceSection(sb, context);
        }
        else
        {
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
        }

        sb.AppendLine();
        sb.AppendLine("Provide:");
        sb.AppendLine("1. Investment decision: GO, CONDITIONAL GO, or NO GO");
        sb.AppendLine("2. Investment thesis (2-3 sentences)");
        sb.AppendLine("3. Conditions precedent (if CONDITIONAL GO)");
        sb.AppendLine("4. Recommended next steps");
        sb.AppendLine();
        sb.AppendLine(ConcisenessDirective);

        return new ClaudeRequest
        {
            SystemPrompt = BuildSystemRole(context.Deal.PropertyType, " Make a GO, CONDITIONAL GO, or NO GO investment decision based on the ZSR Ventures underwriting protocol."),
            UserMessage = sb.ToString(),
            MaxTokens = 1024
        };
    }

    public ClaudeRequest BuildPropertyOverviewPrompt(ProseGenerationContext context)
    {
        var assetType = GetAssetTypeLabel(context.Deal.PropertyType);
        var sb = new StringBuilder();
        sb.AppendLine($"Write a property overview paragraph for the following {assetType} asset.");
        sb.AppendLine();
        AppendPropertyHeader(sb, context);

        sb.AppendLine();
        sb.AppendLine("Write a concise property overview paragraph (3-5 sentences) describing the physical asset, location, and market positioning.");
        sb.AppendLine();
        sb.AppendLine(ConcisenessDirective);

        return new ClaudeRequest
        {
            SystemPrompt = BuildSystemRole(context.Deal.PropertyType, " Write a concise property overview for the underwriting report."),
            UserMessage = sb.ToString(),
            MaxTokens = 256
        };
    }

    // --- Helper methods ---

    private static void AppendCompactPropertyLine(StringBuilder sb, ProseGenerationContext context)
    {
        var deal = context.Deal;
        var isSenior = ProtocolDefaults.IsSeniorHousing(deal.PropertyType);
        var unitLabel = isSenior
            ? $"{deal.LicensedBeds ?? 0} beds"
            : deal.PropertyType == PropertyType.Hospitality
                ? $"{deal.UnitCount} rooms"
                : $"{deal.UnitCount} units";
        sb.AppendLine($"Property: {deal.PropertyName}, {deal.Address}, {unitLabel}");
        sb.AppendLine();
    }

    private static void AppendPropertyHeader(StringBuilder sb, ProseGenerationContext context)
    {
        var deal = context.Deal;
        var isSenior = ProtocolDefaults.IsSeniorHousing(deal.PropertyType);
        sb.AppendLine("## Property Information");
        sb.AppendLine($"- Property: {deal.PropertyName}");
        sb.AppendLine($"- Property Type: {GetAssetTypeLabel(deal.PropertyType)}");
        sb.AppendLine($"- Address: {deal.Address}");
        if (isSenior)
        {
            sb.AppendLine($"- Licensed Beds: {deal.LicensedBeds ?? 0:N0}");
            if (deal.PurchasePrice > 0 && (deal.LicensedBeds ?? 0) > 0)
                sb.AppendLine($"- Price/Bed: {FormatCurrency(deal.PurchasePrice / deal.LicensedBeds!.Value)}");
        }
        else if (deal.PropertyType == PropertyType.Hospitality)
        {
            sb.AppendLine($"- Rooms: {deal.UnitCount:N0}");
            if (deal.PurchasePrice > 0 && deal.UnitCount > 0)
                sb.AppendLine($"- Price/Room: {FormatCurrency(deal.PurchasePrice / deal.UnitCount)}");
        }
        else
        {
            sb.AppendLine($"- Units: {deal.UnitCount:N0}");
            if (deal.PurchasePrice > 0 && deal.UnitCount > 0)
                sb.AppendLine($"- Price/Unit: {FormatCurrency(deal.PurchasePrice / deal.UnitCount)}");
        }
        sb.AppendLine($"- Purchase Price: {FormatCurrency(deal.PurchasePrice)}");
        sb.AppendLine();
    }

    private static void AppendSeniorHousingMetrics(StringBuilder sb, ProseGenerationContext context, bool includeCms = false)
    {
        var deal = context.Deal;
        if (!ProtocolDefaults.IsSeniorHousing(deal.PropertyType)) return;

        sb.AppendLine("## Senior Housing Metrics");
        if (deal.AlBeds.HasValue) sb.AppendLine($"- AL Beds: {deal.AlBeds.Value}");
        if (deal.SnfBeds.HasValue) sb.AppendLine($"- SNF Beds: {deal.SnfBeds.Value}");
        if (deal.MemoryCareBeds.HasValue) sb.AppendLine($"- Memory Care Beds: {deal.MemoryCareBeds.Value}");
        if (deal.AverageDailyRate.HasValue) sb.AppendLine($"- Average Daily Rate: {FormatCurrency(deal.AverageDailyRate.Value)}");
        if (deal.PrivatePayPct.HasValue) sb.AppendLine($"- Private Pay: {deal.PrivatePayPct.Value:N1}%");
        if (deal.MedicaidPct.HasValue) sb.AppendLine($"- Medicaid: {deal.MedicaidPct.Value:N1}%");
        if (deal.MedicarePct.HasValue) sb.AppendLine($"- Medicare: {deal.MedicarePct.Value:N1}%");
        if (deal.StaffingRatio.HasValue) sb.AppendLine($"- Staffing Ratio: {deal.StaffingRatio.Value:N2}");
        if (!string.IsNullOrWhiteSpace(deal.LicenseType)) sb.AppendLine($"- License Type: {deal.LicenseType}");
        if (deal.AverageLengthOfStayMonths.HasValue) sb.AppendLine($"- Average Length of Stay: {deal.AverageLengthOfStayMonths.Value} months");

        // Include CMS data only in Risk Assessment
        if (includeCms && !string.IsNullOrWhiteSpace(deal.CmsData))
        {
            try
            {
                var cms = JsonSerializer.Deserialize<CmsProviderDto>(deal.CmsData);
                if (cms != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("## CMS Care Compare Data");
                    sb.AppendLine($"- Overall Star Rating: {cms.OverallRating}/5");
                    sb.AppendLine($"- Health Inspection Rating: {cms.HealthInspectionRating}/5");
                    sb.AppendLine($"- Staffing Rating: {cms.StaffingRating}/5");
                    sb.AppendLine($"- Quality Measure Rating: {cms.QualityMeasureRating}/5");
                    sb.AppendLine($"- Total Deficiencies: {cms.TotalDeficiencies}");
                    sb.AppendLine($"- Number of Fines: {cms.NumberOfFines} (Total: {FormatCurrency(cms.TotalFinesAmount)})");
                    sb.AppendLine($"- Certified Beds: {cms.CertifiedBeds}");
                    sb.AppendLine($"- RN Hours/Resident/Day: {cms.RnHoursPerResidentDay:N2}");
                    sb.AppendLine($"- Nursing Turnover: {cms.NursingTurnoverPct:N1}%");
                    if (cms.AbuseFlag) sb.AppendLine("- WARNING: Abuse flag present");
                    if (!string.IsNullOrWhiteSpace(cms.SpecialFocusStatus))
                        sb.AppendLine($"- Special Focus Status: {cms.SpecialFocusStatus}");
                }
            }
            catch
            {
                // CMS data parse failure is non-fatal
            }
        }

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

    // --- One-line compliance summary for Executive Summary ---

    private static void AppendFannieComplianceSummaryLine(StringBuilder sb, ProseGenerationContext context)
    {
        if (!context.Deal.FannieProductType.HasValue) return;
        var compliance = DeserializeFannieCompliance(context);
        if (compliance == null) return;

        var parts = new List<string>();
        parts.Add(compliance.OverallPass ? "PASS" : "FAIL");
        if (compliance.DscrTest != null)
            parts.Add($"DSCR {compliance.DscrTest.ActualValue:F2}x vs {compliance.DscrTest.RequiredValue:F2}x min");
        if (compliance.LtvTest != null)
            parts.Add($"LTV {compliance.LtvTest.ActualValue:F0}% vs {compliance.LtvTest.RequiredValue:F0}% max");

        sb.AppendLine($"Fannie Mae Compliance: {string.Join(" | ", parts)}");
    }

    private static void AppendFreddieComplianceSummaryLine(StringBuilder sb, ProseGenerationContext context)
    {
        if (!context.Deal.FreddieProductType.HasValue) return;
        var compliance = DeserializeFreddieCompliance(context);
        if (compliance == null) return;

        var parts = new List<string>();
        parts.Add(compliance.OverallPass ? "PASS" : "FAIL");
        if (compliance.DscrTest != null)
            parts.Add($"DSCR {compliance.DscrTest.ActualValue:F2}x vs {compliance.DscrTest.RequiredValue:F2}x min");
        if (compliance.LtvTest != null)
            parts.Add($"LTV {compliance.LtvTest.ActualValue:F0}% vs {compliance.LtvTest.RequiredValue:F0}% max");

        sb.AppendLine($"Freddie Mac Compliance: {string.Join(" | ", parts)}");
    }

    // --- Fannie Mae compliance helpers ---

    private static void AppendFannieProductHeader(StringBuilder sb, ProseGenerationContext context)
    {
        if (!context.Deal.FannieProductType.HasValue) return;

        var profile = FannieProductProfiles.TryGet(context.Deal.FannieProductType);
        if (profile == null) return;

        sb.AppendLine("## Fannie Mae Execution");
        sb.AppendLine($"- Product Type: {profile.DisplayName}");
        sb.AppendLine($"- Min DSCR: {profile.MinDscr:F2}x");
        sb.AppendLine($"- Max LTV: {profile.MaxLtvPercent:F0}%");
        sb.AppendLine($"- Max Amortization: {profile.MaxAmortizationYears} years");
        if (!string.IsNullOrWhiteSpace(profile.Notes))
            sb.AppendLine($"- Notes: {profile.Notes}");
        sb.AppendLine();
    }

    private static void AppendFannieComplianceSection(StringBuilder sb, ProseGenerationContext context)
    {
        var summary = BuildFannieComplianceSummary(context);
        if (!string.IsNullOrEmpty(summary))
        {
            sb.AppendLine("## Fannie Mae Compliance Results");
            sb.AppendLine(summary);
            sb.AppendLine();
        }
    }

    public static string BuildFannieComplianceSummary(ProseGenerationContext context)
    {
        if (!context.Deal.FannieProductType.HasValue) return string.Empty;

        var compliance = DeserializeFannieCompliance(context);
        if (compliance == null) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine($"Overall Compliance: {(compliance.OverallPass ? "PASS" : "FAIL")}");
        sb.AppendLine($"Product Min DSCR: {compliance.ProductMinDscr:F2}x | Max LTV: {compliance.ProductMaxLtvPercent:F0}% | Max Amort: {compliance.ProductMaxAmortYears} years");
        sb.AppendLine();

        AppendComplianceTest(sb, compliance.DscrTest);
        AppendComplianceTest(sb, compliance.LtvTest);
        AppendComplianceTest(sb, compliance.AmortizationTest);
        AppendComplianceTest(sb, compliance.SeniorsBlendedDscrTest);
        AppendComplianceTest(sb, compliance.CoopActualDscrTest);
        AppendComplianceTest(sb, compliance.CoopMarketRentalDscrTest);
        AppendComplianceTest(sb, compliance.SarmStressDscrTest);
        AppendComplianceTest(sb, compliance.SnfNcfCapTest);
        AppendComplianceTest(sb, compliance.MhcVacancyFloorTest);
        AppendComplianceTest(sb, compliance.RoarRehabDscrTest);
        AppendComplianceTest(sb, compliance.SupplementalCombinedDscrTest);
        AppendComplianceTest(sb, compliance.SupplementalCombinedLtvTest);

        if (compliance.GreenNcfAdjustment.HasValue)
            sb.AppendLine($"- Green NCF Adjustment: {FormatCurrency(compliance.GreenNcfAdjustment.Value)} → Adjusted NCF: {FormatCurrency(compliance.AdjustedNcf ?? 0)}");

        return sb.ToString().TrimEnd();
    }

    private static void AppendComplianceTest(StringBuilder sb, ComplianceTest? test)
    {
        if (test == null) return;
        var status = test.Pass ? "PASS" : "FAIL";
        sb.AppendLine($"- {test.Name}: {status} (actual: {test.ActualValue:F2}, required: {test.RequiredValue:F2}){(test.Notes != null ? $" — {test.Notes}" : "")}");
    }

    private static FannieComplianceResult? DeserializeFannieCompliance(ProseGenerationContext context)
    {
        if (context.Calculations?.FannieComplianceJson is not { } json) return null;
        try
        {
            return JsonSerializer.Deserialize<FannieComplianceResult>(json);
        }
        catch
        {
            return null;
        }
    }

    // --- Freddie Mac compliance helpers ---

    private static void AppendFreddieProductHeader(StringBuilder sb, ProseGenerationContext context)
    {
        if (!context.Deal.FreddieProductType.HasValue) return;

        var profile = FreddieProductProfiles.TryGet(context.Deal.FreddieProductType);
        if (profile == null) return;

        sb.AppendLine("## Freddie Mac Execution");
        sb.AppendLine($"- Product Type: {profile.DisplayName}");
        sb.AppendLine($"- Min DSCR: {profile.MinDscr:F2}x");
        sb.AppendLine($"- Max LTV: {profile.MaxLtvPercent:F0}%");
        sb.AppendLine($"- Max Amortization: {profile.MaxAmortizationYears} years");
        if (!string.IsNullOrWhiteSpace(profile.Notes))
            sb.AppendLine($"- Notes: {profile.Notes}");
        sb.AppendLine();
    }

    private static void AppendFreddieComplianceSection(StringBuilder sb, ProseGenerationContext context)
    {
        var summary = BuildFreddieComplianceSummary(context);
        if (!string.IsNullOrEmpty(summary))
        {
            sb.AppendLine("## Freddie Mac Compliance Results");
            sb.AppendLine(summary);
            sb.AppendLine();
        }
    }

    public static string BuildFreddieComplianceSummary(ProseGenerationContext context)
    {
        if (!context.Deal.FreddieProductType.HasValue) return string.Empty;

        var compliance = DeserializeFreddieCompliance(context);
        if (compliance == null) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine($"Overall Compliance: {(compliance.OverallPass ? "PASS" : "FAIL")}");
        sb.AppendLine($"Product Min DSCR: {compliance.ProductMinDscr:F2}x | Max LTV: {compliance.ProductMaxLtvPercent:F0}% | Max Amort: {compliance.ProductMaxAmortYears} years");
        sb.AppendLine();

        AppendComplianceTest(sb, compliance.DscrTest);
        AppendComplianceTest(sb, compliance.LtvTest);
        AppendComplianceTest(sb, compliance.AmortizationTest);
        AppendComplianceTest(sb, compliance.SblMarketTierTest);
        AppendComplianceTest(sb, compliance.SeniorsBlendedDscrTest);
        AppendComplianceTest(sb, compliance.SnfNoiCapTest);
        AppendComplianceTest(sb, compliance.MhcRentalHomesCapTest);
        AppendComplianceTest(sb, compliance.FloatingRateCapTest);
        AppendComplianceTest(sb, compliance.ValueAddRehabDscrTest);
        AppendComplianceTest(sb, compliance.LeaseUpOccupancyTest);
        AppendComplianceTest(sb, compliance.LeaseUpLeasedTest);
        AppendComplianceTest(sb, compliance.SupplementalCombinedDscrTest);
        AppendComplianceTest(sb, compliance.SupplementalCombinedLtvTest);

        return sb.ToString().TrimEnd();
    }

    private static FreddieComplianceResult? DeserializeFreddieCompliance(ProseGenerationContext context)
    {
        if (context.Calculations?.FreddieComplianceJson is not { } json) return null;
        try
        {
            return JsonSerializer.Deserialize<FreddieComplianceResult>(json);
        }
        catch
        {
            return null;
        }
    }
}
