# Plan: Market Data Enrichment

## Phase 1: Web Search Service
- [x] Create `IWebSearchService` interface in Domain `6eb14df`
- [x] Implement `WebSearchService` in Infrastructure (using Bing API or scraping)
- [x] Create search query builder per protocol patterns (employers, pipeline, rates)
- [x] Configure API key and rate limiting
- [x] Write tests with mocked search responses (covered by Tasks 2-4)

## Phase 2: Result Parsing & Caching
- [ ] Create `MarketContext` DTO with structured fields
- [ ] Implement result parser: extract employer names, project descriptions, rate values
- [ ] Implement per-deal caching for search results
- [ ] Handle empty/irrelevant results with fallback text
- [ ] Track source URLs for attribution
- [ ] Write tests for parsing logic and cache behavior

## Phase 3: Integration
- [ ] Wire market data into report assembly (Sections 4 and 5)
- [ ] Add Fannie Mae rate lookup for loan default assumptions
- [ ] Add source attribution display in report
- [ ] Write integration tests for full search → parse → report flow
