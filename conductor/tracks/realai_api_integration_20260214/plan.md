# Plan: RealAI API Integration

## Phase 1: HTTP Client Setup
- [x] Create `IRealAiClient` interface in Domain with method signatures
- [x] Create `RealAiOptions` configuration class (base URL, API key, timeout)
- [x] Implement `RealAiClient` in Infrastructure using `HttpClient`
- [x] Configure Polly retry policy (3 retries, exponential backoff)
- [x] Register client in DI with typed HttpClient
- [x] Write tests with mocked HTTP responses
[checkpoint: pending]

## Phase 2: Data Retrieval & Mapping
- [ ] Implement property data query (address lookup)
- [ ] Implement tenant metrics query (FICO, HHI, RTI at subject/zip/metro)
- [ ] Implement market data query (cap rates, growth, migration, permits)
- [ ] Implement sales comps query
- [ ] Implement time series query (rent trends, occupancy trends)
- [ ] Create response DTOs matching RealAI API structure
- [ ] Create mapper: RealAI DTOs → `RealAiData` entity
- [ ] Write unit tests for all mappers

## Phase 3: Caching & Error Handling
- [ ] Implement in-memory cache with 24-hour TTL per deal
- [ ] Create `IRealAiCacheService` for cache management
- [ ] Handle API errors: timeout, 401, 404, 500 → graceful degradation
- [ ] Flag unavailable data points in `RealAiData` entity
- [ ] Log all API interactions with Serilog
- [ ] Write tests for cache hit/miss and error scenarios
