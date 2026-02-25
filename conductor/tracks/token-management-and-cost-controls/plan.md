# Plan: Token Management & Cost Controls

## Phase 1: Fix ConversationHistory & Chat Hardening [checkpoint: ee5e980]
- [x] 1.1 Update `ClaudeClient.SendMessageAsync` to map `ConversationHistory` into alternating user/assistant `Messages` array in the API payload
- [x] 1.2 Append `UserMessage` as the final user message after history in the messages array
- [x] 1.3 Add chat send debounce in `DealChatTab.razor` — disable send button while `_isLoading`, enforce 2-second cooldown via `DateTime` check
- [x] 1.4 Unit tests: ClaudeClient sends full conversation history, empty history still works, debounce prevents rapid sends

## Phase 2: Conversation History Truncation [checkpoint: 644b85b]
- [x] 2.1 Create `ConversationTruncator` utility — takes a `List<ConversationMessage>` and returns a truncated list based on max message count (default 20) and estimated token limit (default 150,000)
- [x] 2.2 Token estimation: count characters / 4 as rough token approximation (no external tokenizer dependency)
- [x] 2.3 Wire `ConversationTruncator` into `DealChatTab.razor` — truncate before building `ClaudeRequest`, keeping system prompt + last N messages within budget
- [x] 2.4 Add `TokenManagement` section to `appsettings.json` with `MaxConversationMessages`, `MaxConversationTokens`, `DailyUserTokenBudget`, `DealTokenBudget` config keys
- [x] 2.5 Unit tests: truncation by message count, truncation by token estimate, keeps most recent messages, handles empty history

## Phase 3: Token Usage Tracking [checkpoint: 4fefbea]
- [x] 3.1 Create `TokenUsageRecord` entity — Id, UserId, DealId (nullable), OperationType (enum: Chat, ReportProse, SalesCompExtraction, QuickAnalysis), InputTokens, OutputTokens, Model, CreatedAt
- [x] 3.2 Create `ITokenUsageTracker` interface with `RecordUsageAsync(userId, dealId, operationType, inputTokens, outputTokens, model)`
- [x] 3.3 Implement `TokenUsageTracker` service — persists `TokenUsageRecord` to database, with fire-and-forget pattern (never blocks the caller)
- [x] 3.4 Add EF migration for `TokenUsageRecords` table with indexes on UserId, DealId, CreatedAt
- [x] 3.5 Wire `ITokenUsageTracker` into DealChatTab — record Chat tokens after every Claude response
- [x] 3.6 Wire into `ReportAssembler` — record SalesCompExtraction tokens via SalesCompResult
- [x] 3.7 Wire into `ReportAssembler` — record ReportProse tokens from GeneratedProse totals
- [x] 3.8 Unit tests: usage recorded with correct fields, fire-and-forget doesn't throw on failure, all operation types covered

## Phase 4: Budget Enforcement [checkpoint: 8a8967d]
- [x] 4.1 Create `ITokenBudgetService` interface with `CheckUserBudgetAsync(userId)` returning `(bool allowed, int used, int limit)` and `CheckDealBudgetAsync(dealId)` returning `(bool allowed, int used, int limit, bool warning)`
- [x] 4.2 Implement `TokenBudgetService` — queries `TokenUsageRecords` for daily user totals and lifetime deal totals against configured limits
- [x] 4.3 Wire budget check into `DealChatTab.razor` — check before sending, show "daily limit reached" or "deal token limit reached" message
- [x] 4.4 Wire budget check into report generation path — check before `ReportAssembler` calls prose generator
- [x] 4.5 Admin exemption: skip budget check when user has Admin role
- [x] 4.6 Deal warning at 80%: show a MudAlert banner when deal usage exceeds 80% of budget
- [x] 4.7 Unit tests: budget allows when under limit, blocks when over, admin exempt, 80% warning triggers, daily reset works

## Phase 5: Proper 429 Handling [checkpoint: 2d87df4]
- [x] 5.1 Verify Polly retry policy does NOT retry on HTTP 429 — confirmed `HandleTransientHttpError()` only retries 5xx/408, and our 429→ClaudeRateLimitException preempts `EnsureSuccessStatusCode()`
- [x] 5.2 Create a `ClaudeRateLimitException` that surfaces the Anthropic `retry-after` header value
- [x] 5.3 Catch `ClaudeRateLimitException` in `DealChatTab.razor` (both SendMessage and SendInitialAnalysis) and show "Service is busy, please try again in X seconds" message
- [x] 5.4 Catch in `ReportProseGenerator.GenerateSectionAsync` — add to `failedSections` with rate-limit context instead of generic failure
- [x] 5.5 Unit tests: 429 throws ClaudeRateLimitException, retry-after header parsed, null when no header, 500 still throws HttpRequestException, exception message format

## Phase 6: Admin Token Dashboard
- [x] 6.1 Create `/admin/tokens` page (`AdminTokenDashboard.razor`) with `[Authorize(Roles = "Admin")]`
- [x] 6.2 Add summary cards: Total Tokens, BYOK Tokens, Shared Tokens, BYOK Users
- [x] 6.3 Add usage-by-user table — MudDataGrid with email, BYOK badge, total/BYOK/shared tokens, request count
- [x] 6.4 Add recent usage records table — MudDataGrid with user, operation, model, tokens, BYOK badge, deal link
- [x] 6.5 Add filter panel: user search, key type (BYOK/Shared), operation type, date range
- [x] 6.6 Add navigation link to admin sidebar for the token usage page
- [x] 6.7 bUnit tests: page title renders, BYOK column present, Own Key/Shared badges, key type filter, token counts
