# Implementation Plan: Securitization Comps — SEC EDGAR + Agency Data

## Phase 1: Domain Model & Data Storage

- [x] Task: Create `SecuritizationComp` entity with fields: Source (CMBS/FannieMae/FreddieMac), PropertyType, State, City, MSA, Units, LoanAmount, InterestRate, DSCR, LTV, NOI, Occupancy, CapRate, MaturityDate, OriginationDate, DealName, SecuritizationId `29968db`
- [x] Task: Create `SecuritizationDataSource` enum (CMBS, FannieMae, FreddieMac) `29968db`
- [x] Task: Add `DbSet<SecuritizationComp>` to AppDbContext with EF config (indexes on PropertyType, State, OriginationDate) `29968db`
- [x] Task: Create EF Core migration for SecuritizationComps table `29968db`
- [x] Task: Write unit tests for entity construction and validation `29968db`
- [x] Task: Phase 1 Manual Verification `29968db`

## Phase 2: Bulk Data Import — All Sources

Unified bulk import architecture: all three data sources (CMBS, Fannie Mae, Freddie Mac) are imported into the local SecuritizationComps table. Data is refreshed monthly via admin endpoint or background job.

- [x] Task: Create `IEdgarCmbsClient` interface with `FetchRecentFilingsAsync(int monthsBack)` returning parsed comps from ABS-EE EX-102 XML `af17026`
- [x] Task: Implement `EdgarCmbsClient` — search EDGAR EFTS for ABS-EE filings, download EX-102 XML exhibits, parse loan-level CMBS fields `af17026`
- [x] Task: Implement EX-102 XML parser to extract: propertyTypeCode, mostRecentDebtServiceCoverageNOI, mostRecentNOIAmount, mostRecentValuationAmount, originalLoanAmount, originalInterestRatePercentage, mostRecentPhysicalOccupancy, numberOfUnitsBedsRooms, propertyState, propertyCity, originationDate, maturityDate `af17026`
- [x] Task: Map CMBS property type codes to app PropertyType enum (MF→Multifamily, HC→SeniorHousing, MH→ManufacturedHousing) `af17026`
- [x] Task: Create `IAgencyDataImporter` interface with `ImportFannieMaeCsvAsync(Stream)` and `ImportFreddieMacCsvAsync(Stream)` `af17026`
- [x] Task: Implement Fannie Mae CSV parser — map MFLPD columns to SecuritizationComp entity `af17026`
- [x] Task: Implement Freddie Mac CSV parser (same pattern, different column mapping) `af17026`
- [x] Task: Implement batch insert with chunked AddRange (5K batch size) for all sources `af17026`
- [x] Task: Add admin-only import endpoint: `POST /api/admin/import-securitization-data` with source selector and file upload for CSV sources `af17026`
- [x] Task: Add Polly retry policy for SEC EDGAR rate limiting (10 req/sec) and register HttpClient in DI `af17026`
- [x] Task: Write unit tests: EDGAR XML parsing with fixtures, CSV parsing with fixtures, batch insert logic `af17026`
- [x] Task: Phase 2 Manual Verification `af17026`

## Phase 3: Comp Matching Service

- [x] Task: Create `ISecuritizationCompService` interface with `FindCompsAsync(deal, maxResults)` returning ranked comps `dd5f015`
- [x] Task: Implement comp matching logic: filter by property type + state, then rank by similarity score (unit count proximity, recency, DSCR range) `dd5f015`
- [x] Task: Add fallback: if <5 comps in same state, expand to adjacent states or nationwide for same property type `dd5f015`
- [x] Task: Create `ComparisonResult` DTO with user metrics vs comp median/average/range `dd5f015`
- [x] Task: Write unit tests for matching and ranking logic with edge cases (no comps, exact matches, cross-state fallback) `dd5f015`
- [x] Task: Phase 3 Manual Verification `dd5f015`

## Phase 4: UI — Comparison Card

- [x] Task: Add `ISecuritizationCompService` to DealTabs DI and call `FindCompsAsync` when loading Underwriting tab `70b7718`
- [x] Task: Create securitization comps comparison card on Underwriting tab showing: user metric | market median | market range for DSCR, LTV, Cap Rate, Occupancy, Rate `70b7718`
- [x] Task: Add expandable detail table showing individual comp loans (source, deal name, state, units, DSCR, LTV, rate, date) `70b7718`
- [x] Task: Color-code user metrics vs market: green if within range, amber if near boundary, red if outside `70b7718`
- [x] Task: Write bUnit tests for comp card rendering `70b7718`
- [x] Task: Phase 4 Manual Verification `70b7718`

## Phase 5: Prompt Integration

- [x] Task: Add `SecuritizationComps` property to `ProseGenerationContext` — list of top 5 matched comps `21a0940`
- [x] Task: Create `AppendSecuritizationComps(StringBuilder, comps)` helper in `UnderwritingPromptBuilder` — formats comp summary as "Market Benchmarks" section `21a0940`
- [x] Task: Include comp benchmarks in Executive Summary, Risk Assessment, and Investment Decision prompts `21a0940`
- [x] Task: Update Investment Decision GO/NO GO logic to reference market positioning ("DSCR exceeds market median by X basis points") `21a0940`
- [x] Task: Write unit tests for prompt content with and without comps `21a0940`
- [x] Task: Phase 5 Manual Verification `21a0940`

---
