# Plan: RealAI API Integration

## Phase 1: HTTP Client Setup
- [x] Create `IRealAiClient` interface in Domain with method signatures
- [x] Create `RealAiOptions` configuration class (base URL, API key, timeout)
- [x] Implement `RealAiClient` in Infrastructure using `HttpClient`
- [x] Configure Polly retry policy (3 retries, exponential backoff)
- [x] Register client in DI with typed HttpClient
- [x] Write tests with mocked HTTP responses
[checkpoint: 40d5111]

## Phase 2: Data Retrieval & Mapping
- [x] Implement property data query (address lookup)
- [x] Implement tenant metrics query (FICO, HHI, RTI at subject/zip/metro)
- [x] Implement market data query (cap rates, growth, migration, permits)
- [x] Implement sales comps query
- [x] Implement time series query (rent trends, occupancy trends)
- [x] Create response DTOs matching RealAI API structure
- [x] Create mapper: RealAI DTOs → `RealAiData` entity
- [x] Write unit tests for all mappers
[checkpoint: f1837a7]

## Phase 3: Caching & Error Handling
- [x] Implement in-memory cache with 24-hour TTL per deal
- [x] Create `IRealAiCacheService` for cache management
- [x] Handle API errors: timeout, 401, 404, 500 → graceful degradation
- [x] Flag unavailable data points in `RealAiData` entity
- [x] Log all API interactions with Serilog
- [x] Write tests for cache hit/miss and error scenarios
[checkpoint: 6e3f3ca]
