# Plan: Report & Analysis Enhancements

## Phase 1: Financial Analysis Completion
- [x] ce25d56 1.1 Update `ReportAssembler.BuildFinancialAnalysis()` to call `UnderwritingCalculator.ProjectNoi()` and `ProjectCashFlows()` for 5-year projections
- [x] ce25d56 1.2 Populate `FiveYearCashFlow[]` with year-by-year EGI, OpEx, NOI, DebtService, CashFlow, CashOnCash
- [x] ce25d56 1.3 Populate `ReturnsAnalysis` (IRR, EquityMultiple, AverageCashOnCash, TotalProfit) using calculator methods
- [x] ce25d56 1.4 Populate `ExitAnalysis` (ExitCapRate, ExitNoi, ExitValue, LoanBalance, NetProceeds)
- [x] ce25d56 1.5 Unit tests for populated financial sections

## Phase 2: Sensitivity Analysis UI
- [x] 7a64806 2.1 Add `ISensitivityCalculator` interface wrapping the existing static `SensitivityCalculator`
- [x] 7a64806 2.2 Register in DI and wire into DealTabs
- [x] 7a64806 2.3 Add sensitivity matrix section to the Underwriting tab in `DealTabs.razor` — MudSimpleTable showing 4 scenarios with NOI, NOI delta, exit value, exit value delta
- [x] 7a64806 2.4 Color-code deltas (green positive, red negative)
- [x] 7a64806 2.5 bUnit test for sensitivity section rendering

## Phase 3: Market Data Wiring
- [x] f87005e 3.1 Inject `MarketDataService` into `ReportAssembler` (add to constructor)
- [x] f87005e 3.2 Call `GetMarketContextAsync(city, state)` during report assembly using address parsing
- [x] f87005e 3.3 Call `MarketDataEnricher.EnrichPropertyComps()` to populate PropertyComps narrative and source attribution
- [x] f87005e 3.4 Call `MarketDataEnricher.EnrichTenantMarket()` to populate TenantMarket narrative and benchmarks
- [x] f87005e 3.5 Unit tests for market-enriched report sections

## Phase 4: Sales Comps Extraction
- [x] 0c7f291 4.1 Create `ISalesCompExtractor` service interface with `Task<SalesCompResult> ExtractCompsAsync(MarketContextDto context, string subjectAddress, ...)`
- [x] 0c7f291 4.2 Implement `SalesCompExtractor` — sends ComparableTransactions web search results to Claude with a structured extraction prompt, parses response into `SalesCompRow[]`
- [x] 0c7f291 4.3 Wire into `ReportAssembler` — call after market data, populate `PropertyCompsSection.Comps`
- [x] 0c7f291 4.4 Add adjustments logic — Claude generates `AdjustmentRow[]` comparing each comp to the subject
- [x] 0c7f291 4.5 Unit tests with mock Claude response

## Phase 5: Public API Enrichment
- [x] e774315 5.1 Create `IPublicDataService` interface with methods: `GetCensusDataAsync(string zipCode)`, `GetBlsDataAsync(string state, string metro)`, `GetFredDataAsync()`
- [x] e774315 5.2 Implement `CensusApiClient` — call Census Bureau ACS API for HHI, population, demographics by zip/tract
- [x] e774315 5.3 Implement `BlsApiClient` — call BLS API for unemployment rate, job growth by metro area
- [x] e774315 5.4 Implement `FredApiClient` — call FRED API for rent index, CPI, treasury rates
- [x] e774315 5.5 Create `PublicDataDto` to hold aggregated results from all 3 sources
- [x] e774315 5.6 Register services in DI, add HttpClient registrations in `Program.cs`
- [x] e774315 5.7 Wire into `ReportAssembler` — enrich market context with public data before prose generation
- [x] e774315 5.8 Unit tests with mocked HTTP responses

## Phase 6: Tenant Demographics
- [x] 2046aec 6.1 Extend `CensusApiClient` to pull ACS tenant demographics: median HHI, age distribution, household size, rent burden %
- [x] 2046aec 6.2 Create `TenantDemographicsDto` with structured fields
- [x] 2046aec 6.3 Wire into `MarketDataEnricher.EnrichTenantMarket()` — add Census benchmarks to `BenchmarkRow[]` (Subject vs Market median)
- [x] 2046aec 6.4 Display demographics in TenantMarket report section with source attribution ("U.S. Census Bureau ACS")
- [x] 2046aec 6.5 Unit tests for demographics enrichment

## Phase 7: Report Prose Generation
- [x] 051946f 7.1 Inject `IReportProseGenerator` into `ReportAssembler`
- [x] 051946f 7.2 Build `ProseGenerationContext` from deal data, calculations, market context, and public data
- [x] 051946f 7.3 Call `GenerateAllProseAsync()` and map `GeneratedProse` sections into the 6 placeholder sections (ExecutiveSummary, PropertyComps, TenantMarket, ValueCreation, RiskAssessment, InvestmentDecision)
- [x] 051946f 7.4 Populate `KeyHighlights[]`, `KeyRisks[]`, `Risks[]`, `Conditions[]`, `NextSteps[]` from prose output (parse or use Claude structured output)
- [x] 051946f 7.5 Handle prose generation failures gracefully — fall back to calculation-only report
- [x] 051946f 7.6 Unit tests with mocked prose generator

## Phase 8: UI Integration
- [ ] 8.1 Add "Generate Report" button to DealTabs header — triggers `ReportAssembler.AssembleReportAsync()` then navigates to `/deals/{id}/report`
- [ ] 8.2 Add loading state / progress indicator while report generates (prose + market data can take 30-60s)
- [ ] 8.3 Feed `MarketContextDto` into `DealChatTab`'s `BuildSystemPrompt()` so Claude has market context during conversations
- [ ] 8.4 Add market data summary to chat welcome message when available
- [ ] 8.5 End-to-end manual test: search → chat → generate report → view → export PDF
