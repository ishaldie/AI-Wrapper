# Spec: Report & Analysis Enhancements

## Overview
Wire together existing but disconnected components to complete the underwriting report pipeline. `ReportProseGenerator`, `ReportPdfExporter`, `SensitivityCalculator`, `MarketDataService`, and `MarketDataEnricher` are all fully implemented but `ReportAssembler` doesn't call any of them — it returns placeholder text for 7/10 sections. This track connects the dots and adds public API data enrichment.

## Requirements

1. **ReportAssembler → ReportProseGenerator integration**: Wire `ReportAssembler` to call `IReportProseGenerator.GenerateAllProseAsync()` and populate ExecutiveSummary, PropertyComps, TenantMarket, ValueCreation, RiskAssessment, and InvestmentDecision narratives with Claude-generated prose instead of "[AI-generated ... pending]" placeholders.

2. **ReportAssembler → MarketDataEnricher integration**: Wire `ReportAssembler` to call `MarketDataService.GetMarketContextAsync()` and `MarketDataEnricher` to populate PropertyComps comps data and TenantMarket benchmarks with web search results.

3. **ReportAssembler → FinancialAnalysis completion**: Populate the empty `FiveYearCashFlow[]`, `Returns`, and `Exit` sections in `BuildFinancialAnalysis()` using the existing `UnderwritingCalculator` projection methods.

4. **Sales Comps extraction**: Use Claude to parse web search results from the `ComparableTransactions` category into structured `SalesCompRow[]` (address, sale price, units, price/unit, cap rate, date, distance).

5. **Sensitivity Analysis UI**: Add a sensitivity analysis section to the Underwriting tab in DealTabs showing a scenario matrix (Base Case, Income -5%, Occupancy -10%, Cap Rate +100bps) with NOI and exit value deltas, using the existing `SensitivityCalculator`.

6. **Report generation trigger**: Add a "Generate Report" button on the deal page that kicks off `ReportAssembler` (which now calls prose generator + market enrichment) and navigates to the report view.

7. **Market data integration into chat context**: Feed `MarketContextDto` data into `DealChatTab`'s system prompt so Claude has market context during deal conversations.

8. **Public API market data enrichment**: Integrate free public APIs to provide structured market metrics:
   - **Census Bureau API** → Median household income (HHI), population growth, demographics, rent-to-income ratios
   - **BLS API** → Job growth %, unemployment rate, major employment sectors
   - **FRED API** → Rent index trends, CPI, interest rate history
   - Store results in the existing `MarketContextDto` and feed into report sections.

9. **Tenant demographics from Census**: Pull tenant demographic data (median HHI, age distribution, household size, rent burden %) from Census Bureau ACS data for the property's zip code/tract. Display in TenantMarket report section benchmarks.

## Acceptance Criteria
- Report sections display Claude-generated prose instead of placeholders
- PropertyComps section shows structured sales comparable data from web search
- TenantMarket section shows market benchmarks with source attribution
- TenantMarket includes Census-sourced demographic data (HHI, rent burden, household size)
- FinancialAnalysis shows 5-year cash flows, returns, and exit analysis
- Sensitivity analysis matrix visible on Underwriting tab
- PDF export includes all populated sections (already working via QuestPDF)
- "Generate Report" button on deal page triggers full pipeline
- Market data from Census/BLS/FRED populates report context
- All new integration points have unit tests

## Out of Scope
- Custom sensitivity scenario configuration — use the 4 hardcoded scenarios for now
- Report caching/versioning — generate fresh each time
- Paid data source integrations (CoStar, LoopNet, Yardi)
- Historical deal performance tracking / portfolio analytics
