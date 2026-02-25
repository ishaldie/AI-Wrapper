# Plan: Token Management & Cost Controls

## Phase 1: Fix ConversationHistory & Chat Hardening [checkpoint: ee5e980]
- [x] 1.1 Update `ClaudeClient.SendMessageAsync` to map `ConversationHistory` into alternating user/assistant `Messages` array in the API payload
- [x] 1.2 Append `UserMessage` as the final user message after history in the messages array
- [x] 1.3 Add chat send debounce in `DealChatTab.razor` — disable send button while `_isLoading`, enforce 2-second cooldown via `DateTime` check
- [x] 1.4 Unit tests: ClaudeClient sends full conversation history, empty history still works, debounce prevents rapid sends

## Phase 2: Conversation History Truncation
- [x] 2.1 Create `ConversationTruncator` utility — takes a `List<ConversationMessage>` and returns a truncated list based on max message count (default 20) and estimated token limit (default 150,000)
- [x] 2.2 Token estimation: count characters / 4 as rough token approximation (no external tokenizer dependency)
- [x] 2.3 Wire `ConversationTruncator` into `DealChatTab.razor` — truncate before building `ClaudeRequest`, keeping system prompt + last N messages within budget
- [x] 2.4 Add `TokenManagement` section to `appsettings.json` with `MaxConversationMessages`, `MaxConversationTokens`, `DailyUserTokenBudget`, `DealTokenBudget` config keys
- [x] 2.5 Unit tests: truncation by message count, truncation by token estimate, keeps most recent messages, handles empty history

## Phase 3: Token Usage Tracking
- [ ] 3.1 Create `TokenUsageRecord` entity — Id, UserId, DealId (nullable), OperationType (enum: Chat, ReportProse, SalesCompExtraction, QuickAnalysis), InputTokens, OutputTokens, Model, CreatedAt
- [ ] 3.2 Create `ITokenUsageTracker` interface with `RecordUsageAsync(userId, dealId, operationType, inputTokens, outputTokens, model)`
- [ ] 3.3 Implement `TokenUsageTracker` service — persists `TokenUsageRecord` to database, with fire-and-forget pattern (never blocks the caller)
- [ ] 3.4 Add EF migration for `TokenUsageRecords` table with indexes on UserId, DealId, CreatedAt
- [ ] 3.5 Wire `ITokenUsageTracker` into `ClaudeClient` — record after every successful API call
- [ ] 3.6 Wire into `SalesCompExtractor` — record extraction call tokens
- [ ] 3.7 Wire into `ReportProseGenerator` — record per-section tokens with `ReportProse` operation type
- [ ] 3.8 Unit tests: usage recorded with correct fields, fire-and-forget doesn't throw on failure, all operation types covered

## Phase 4: Budget Enforcement
- [ ] 4.1 Create `ITokenBudgetService` interface with `CheckUserBudgetAsync(userId)` returning `(bool allowed, int used, int limit)` and `CheckDealBudgetAsync(dealId)` returning `(bool allowed, int used, int limit, bool warning)`
- [ ] 4.2 Implement `TokenBudgetService` — queries `TokenUsageRecords` for daily user totals and lifetime deal totals against configured limits
- [ ] 4.3 Wire budget check into `DealChatTab.razor` — check before sending, show "daily limit reached" or "deal token limit reached" message
- [ ] 4.4 Wire budget check into report generation path — check before `ReportAssembler` calls prose generator
- [ ] 4.5 Admin exemption: skip budget check when user has Admin role
- [ ] 4.6 Deal warning at 80%: show a MudAlert banner when deal usage exceeds 80% of budget
- [ ] 4.7 Unit tests: budget allows when under limit, blocks when over, admin exempt, 80% warning triggers, daily reset works

## Phase 5: Proper 429 Handling
- [ ] 5.1 Update Polly retry policy in `ClaudeClient` — do NOT retry on HTTP 429, only retry on 5xx and transient network errors
- [ ] 5.2 Create a `ClaudeRateLimitException` that surfaces the Anthropic `retry-after` header value
- [ ] 5.3 Catch `ClaudeRateLimitException` in `DealChatTab.razor` and show "Service is busy, please try again in X seconds" message
- [ ] 5.4 Catch in `ReportProseGenerator.GenerateSectionAsync` — add to `failedSections` with rate-limit context instead of generic failure
- [ ] 5.5 Unit tests: 429 not retried, retry-after header parsed, 500 still retried, user-facing message shown

## Phase 6: Admin Token Dashboard
- [ ] 6.1 Create `/admin/tokens` page (`AdminTokenUsage.razor`) with `[Authorize(Roles = "Admin")]`
- [ ] 6.2 Add summary cards: total tokens today, total tokens this month, estimated cost (configurable $/M input and $/M output rates from config)
- [ ] 6.3 Add usage-by-user table — MudDataGrid showing user email, daily tokens, monthly tokens, deal count
- [ ] 6.4 Add usage-by-deal table — MudDataGrid showing deal name, total tokens, operation breakdown (chat vs report vs extraction)
- [ ] 6.5 Add daily usage chart — MudChart (bar) showing tokens per day for the last 30 days
- [ ] 6.6 Add navigation link to admin sidebar for the token usage page
- [ ] 6.7 bUnit test: dashboard renders with admin role, rejects non-admin access
