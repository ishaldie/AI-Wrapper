# Implementation Plan: Securitization Comps — SEC EDGAR + Agency Data

## Phase 1: Domain Model & Data Storage

- [ ] Task: Create `SecuritizationComp` entity with fields: Source (CMBS/FannieMae/FreddieMac), PropertyType, State, City, MSA, Units, LoanAmount, InterestRate, DSCR, LTV, NOI, Occupancy, CapRate, MaturityDate, OriginationDate, DealName, SecuritizationId
- [ ] Task: Create `SecuritizationDataSource` enum (CMBS, FannieMae, FreddieMac)
- [ ] Task: Add `DbSet<SecuritizationComp>` to AppDbContext with EF config (indexes on PropertyType, State, OriginationDate)
- [ ] Task: Create EF Core migration for SecuritizationComps table
- [ ] Task: Write unit tests for entity construction and validation
- [ ] Task: Phase 1 Manual Verification

## Phase 2: SEC EDGAR CMBS Client

- [ ] Task: Create `IEdgarCmbsClient` interface with `SearchCmbsFilingsAsync(state, propertyType, dateRange)` and `ParseEx102XmlAsync(filingUrl)`
- [ ] Task: Implement `EdgarCmbsClient` — HTTP GET to `efts.sec.gov/LATEST/search-index` to find ABS-EE filings, then fetch EX-102 XML exhibits
- [ ] Task: Implement EX-102 XML parser to extract loan-level fields: `property_type_code`, `most_recent_dsc_noi_percentage`, `most_recent_noi_amount`, `most_recent_valuation_amount`, `original_loan_amount`, `original_interest_rate_percentage`, `most_recent_physical_occupancy_percentage`, `units_beds_rooms`, `property_state`, `property_city`
- [ ] Task: Map CMBS property type codes to app PropertyType enum (MF→Multifamily, HC→SeniorHousing, MH→ManufacturedHousing, etc.)
- [ ] Task: Add Polly retry policy for SEC EDGAR rate limiting (10 req/sec limit)
- [ ] Task: Write unit tests with sample EX-102 XML fixtures
- [ ] Task: Phase 2 Manual Verification

## Phase 3: Agency Data Import (Fannie Mae CSV)

- [ ] Task: Create `IAgencyDataImporter` interface with `ImportFannieMaeCsvAsync(Stream csvStream)` and `ImportFreddieMacCsvAsync(Stream csvStream)`
- [ ] Task: Implement Fannie Mae CSV parser — map 62 MFLPD columns to `SecuritizationComp` entity (DSCR, LTV, property type, units, state, loan amount, rate, occupancy)
- [ ] Task: Implement batch insert with `BulkExtensions` or chunked `AddRange` (71K+ rows, 5K batch size)
- [ ] Task: Add admin-only import endpoint: `POST /api/admin/import-securitization-data` with file upload
- [ ] Task: Implement Freddie Mac CSV parser (same pattern, different column mapping)
- [ ] Task: Write unit tests for CSV parsing with sample fixture data
- [ ] Task: Phase 3 Manual Verification

## Phase 4: Comp Matching Service

- [ ] Task: Create `ISecuritizationCompService` interface with `FindCompsAsync(deal, maxResults)` returning ranked comps
- [ ] Task: Implement comp matching logic: filter by property type + state, then rank by similarity score (unit count proximity, recency, DSCR range)
- [ ] Task: Add fallback: if <5 comps in same state, expand to adjacent states or nationwide for same property type
- [ ] Task: Create `ComparisonResult` DTO with user metrics vs comp median/average/range
- [ ] Task: Write unit tests for matching and ranking logic with edge cases (no comps, exact matches, cross-state fallback)
- [ ] Task: Phase 4 Manual Verification

## Phase 5: UI — Comparison Card

- [ ] Task: Add `ISecuritizationCompService` to DealTabs DI and call `FindCompsAsync` when loading Underwriting tab
- [ ] Task: Create securitization comps comparison card on Underwriting tab showing: user metric | market median | market range for DSCR, LTV, Cap Rate, Occupancy, Rate
- [ ] Task: Add expandable detail table showing individual comp loans (source, deal name, state, units, DSCR, LTV, rate, date)
- [ ] Task: Color-code user metrics vs market: green if within range, amber if near boundary, red if outside
- [ ] Task: Write bUnit tests for comp card rendering
- [ ] Task: Phase 5 Manual Verification

## Phase 6: Prompt Integration

- [ ] Task: Add `SecuritizationComps` property to `ProseGenerationContext` — list of top 5 matched comps
- [ ] Task: Create `AppendSecuritizationComps(StringBuilder, comps)` helper in `UnderwritingPromptBuilder` — formats comp summary as "Market Benchmarks" section
- [ ] Task: Include comp benchmarks in Executive Summary, Risk Assessment, and Investment Decision prompts
- [ ] Task: Update Investment Decision GO/NO GO logic to reference market positioning ("DSCR exceeds market median by X basis points")
- [ ] Task: Write unit tests for prompt content with and without comps
- [ ] Task: Phase 6 Manual Verification

---
