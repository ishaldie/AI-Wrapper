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

- [ ] Task: Create `IEdgarCmbsClient` interface with `FetchRecentFilingsAsync(int monthsBack)` returning parsed comps from ABS-EE EX-102 XML
- [ ] Task: Implement `EdgarCmbsClient` — search EDGAR EFTS for ABS-EE filings, download EX-102 XML exhibits, parse loan-level CMBS fields
- [ ] Task: Implement EX-102 XML parser to extract: propertyTypeCode, mostRecentDebtServiceCoverageNOI, mostRecentNOIAmount, mostRecentValuationAmount, originalLoanAmount, originalInterestRatePercentage, mostRecentPhysicalOccupancy, numberOfUnitsBedsRooms, propertyState, propertyCity, originationDate, maturityDate
- [ ] Task: Map CMBS property type codes to app PropertyType enum (MF→Multifamily, HC→SeniorHousing, MH→ManufacturedHousing)
- [ ] Task: Create `IAgencyDataImporter` interface with `ImportFannieMaeCsvAsync(Stream)` and `ImportFreddieMacCsvAsync(Stream)`
- [ ] Task: Implement Fannie Mae CSV parser — map MFLPD columns to SecuritizationComp entity
- [ ] Task: Implement Freddie Mac CSV parser (same pattern, different column mapping)
- [ ] Task: Implement batch insert with chunked AddRange (5K batch size) for all sources
- [ ] Task: Add admin-only import endpoint: `POST /api/admin/import-securitization-data` with source selector and file upload for CSV sources
- [ ] Task: Add Polly retry policy for SEC EDGAR rate limiting (10 req/sec) and register HttpClient in DI
- [ ] Task: Write unit tests: EDGAR XML parsing with fixtures, CSV parsing with fixtures, batch insert logic
- [ ] Task: Phase 2 Manual Verification

## Phase 3: Comp Matching Service

- [ ] Task: Create `ISecuritizationCompService` interface with `FindCompsAsync(deal, maxResults)` returning ranked comps
- [ ] Task: Implement comp matching logic: filter by property type + state, then rank by similarity score (unit count proximity, recency, DSCR range)
- [ ] Task: Add fallback: if <5 comps in same state, expand to adjacent states or nationwide for same property type
- [ ] Task: Create `ComparisonResult` DTO with user metrics vs comp median/average/range
- [ ] Task: Write unit tests for matching and ranking logic with edge cases (no comps, exact matches, cross-state fallback)
- [ ] Task: Phase 3 Manual Verification

## Phase 4: UI — Comparison Card

- [ ] Task: Add `ISecuritizationCompService` to DealTabs DI and call `FindCompsAsync` when loading Underwriting tab
- [ ] Task: Create securitization comps comparison card on Underwriting tab showing: user metric | market median | market range for DSCR, LTV, Cap Rate, Occupancy, Rate
- [ ] Task: Add expandable detail table showing individual comp loans (source, deal name, state, units, DSCR, LTV, rate, date)
- [ ] Task: Color-code user metrics vs market: green if within range, amber if near boundary, red if outside
- [ ] Task: Write bUnit tests for comp card rendering
- [ ] Task: Phase 4 Manual Verification

## Phase 5: Prompt Integration

- [ ] Task: Add `SecuritizationComps` property to `ProseGenerationContext` — list of top 5 matched comps
- [ ] Task: Create `AppendSecuritizationComps(StringBuilder, comps)` helper in `UnderwritingPromptBuilder` — formats comp summary as "Market Benchmarks" section
- [ ] Task: Include comp benchmarks in Executive Summary, Risk Assessment, and Investment Decision prompts
- [ ] Task: Update Investment Decision GO/NO GO logic to reference market positioning ("DSCR exceeds market median by X basis points")
- [ ] Task: Write unit tests for prompt content with and without comps
- [ ] Task: Phase 5 Manual Verification

---
