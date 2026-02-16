# Plan: Claude AI Report Generation

## Phase 1: Claude API Client
- [x] Create `IClaudeClient` interface in Domain
- [x] Create `ClaudeOptions` configuration class (API key, model, max tokens)
- [x] Implement `ClaudeClient` in Infrastructure using Anthropic SDK or HttpClient
- [x] Configure Polly retry policy for API calls
- [x] Implement token usage tracking and logging
- [x] Write tests with mocked API responses

## Phase 2: Prompt Engineering
- [ ] Create `IPromptBuilder` interface for structured prompt construction
- [ ] Build prompt template for Executive Summary (decision badge, thesis, risks)
- [ ] Build prompt template for Market Context (supply-demand, economics, rent outlook)
- [ ] Build prompt template for Value Creation Strategy (timeline, capital)
- [ ] Build prompt template for Risk Assessment narratives
- [ ] Build prompt template for Investment Decision (GO/NO GO, conditions, next steps)
- [ ] Build prompt template for Property Overview prose
- [ ] Write tests verifying prompt templates include all required data fields

## Phase 3: Response Parsing & Assembly
- [ ] Create `IReportProseGenerator` service that orchestrates all prompts
- [ ] Parse Claude responses into structured prose sections
- [ ] Validate GO/NO GO decision against protocol thresholds
- [ ] Handle partial failures (some sections fail, others succeed)
- [ ] Create `GeneratedProse` DTO with all 6 sections
- [ ] Write integration tests for full prose generation pipeline
