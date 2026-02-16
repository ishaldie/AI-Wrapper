# Plan: Claude AI Report Generation

## Phase 1: Claude API Client
- [x] Create `IClaudeClient` interface in Domain
- [x] Create `ClaudeOptions` configuration class (API key, model, max tokens)
- [x] Implement `ClaudeClient` in Infrastructure using Anthropic SDK or HttpClient
- [x] Configure Polly retry policy for API calls
- [x] Implement token usage tracking and logging
- [x] Write tests with mocked API responses

## Phase 2: Prompt Engineering
- [x] Create `IPromptBuilder` interface for structured prompt construction
- [x] Build prompt template for Executive Summary (decision badge, thesis, risks)
- [x] Build prompt template for Market Context (supply-demand, economics, rent outlook)
- [x] Build prompt template for Value Creation Strategy (timeline, capital)
- [x] Build prompt template for Risk Assessment narratives
- [x] Build prompt template for Investment Decision (GO/NO GO, conditions, next steps)
- [x] Build prompt template for Property Overview prose
- [x] Write tests verifying prompt templates include all required data fields

## Phase 3: Response Parsing & Assembly
- [ ] Create `IReportProseGenerator` service that orchestrates all prompts
- [ ] Parse Claude responses into structured prose sections
- [ ] Validate GO/NO GO decision against protocol thresholds
- [ ] Handle partial failures (some sections fail, others succeed)
- [ ] Create `GeneratedProse` DTO with all 6 sections
- [ ] Write integration tests for full prose generation pipeline
