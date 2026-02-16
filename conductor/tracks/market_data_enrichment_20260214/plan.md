# Plan: Market Data Enrichment

## Phase 1: Web Search Service
- [x] Create `IWebSearchService` interface in Domain `6eb14df`
- [x] Implement `WebSearchService` in Infrastructure (using Bing API or scraping)
- [x] Create search query builder per protocol patterns (employers, pipeline, rates)
- [x] Configure API key and rate limiting
- [x] Write tests with mocked search responses (covered by Tasks 2-4)
[checkpoint: e57c55c]

## Phase 2: Result Parsing & Caching
- [x] Create `MarketContext` DTO with structured fields
- [x] Implement result parser: extract employer names, project descriptions, rate values
- [x] Implement per-deal caching for search results
- [x] Handle empty/irrelevant results with fallback text
- [x] Track source URLs for attribution
- [x] Write tests for parsing logic and cache behavior
[checkpoint: 21fed99]

## Phase 3: Integration
- [x] Wire market data into report assembly (Sections 4 and 5) `89c8dc8`
- [x] Add Fannie Mae rate lookup for loan default assumptions `89c8dc8`
- [x] Add source attribution display in report `89c8dc8`
- [x] Write integration tests for full search → parse → report flow `89c8dc8`
