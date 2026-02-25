# Spec: Token Management & Cost Controls

## Overview
Add token budgeting, cost tracking, conversation history management, and rate limiting for Claude API usage. Currently the application has per-request MaxTokens caps and records per-message token counts, but has no enforcement of budgets, no conversation truncation, a critical bug where ConversationHistory is silently dropped in HTTP API mode, and no visibility into aggregate token spend. This track adds the guardrails needed to control API costs in production.

## Requirements

1. **Fix ConversationHistory in ClaudeClient**: Update the HTTP `ClaudeClient` to actually send `ConversationHistory` as alternating user/assistant messages in the API payload. Multi-turn chat is currently broken in production API mode.

2. **Conversation history truncation**: Implement a sliding window that keeps the last N messages (configurable, default 20) or truncates when estimated input tokens exceed a threshold (configurable, default 150,000). Older messages are dropped from the API call but remain in the database.

3. **Chat send debounce**: Add a cooldown in `DealChatTab.razor` — disable the send button while a response is pending, and enforce a minimum 2-second gap between sends to prevent rapid-fire API calls.

4. **Per-user daily token budget**: Create a `TokenBudgetService` that tracks daily token consumption per user. Configurable daily limit (default 500,000 tokens). When exceeded, return a friendly "daily limit reached" message instead of calling the API. Admin users are exempt.

5. **Per-deal token budget**: Track cumulative token usage per deal. Configurable limit (default 1,000,000 tokens). Warn at 80% usage, block at 100%.

6. **Token usage persistence**: Create a `TokenUsageRecord` entity that logs every Claude API call with userId, dealId, operation type (chat/report/extraction), input tokens, output tokens, model, and timestamp. Persist from `ClaudeClient` via a new `ITokenUsageTracker` interface.

7. **Proper 429 handling**: Stop retrying on HTTP 429 from Anthropic. Instead, surface a "service is busy, please try again in a moment" message to the user. Only retry on 5xx errors.

8. **SalesCompExtractor token tracking**: Wire token usage from the sales comp extraction call into the same tracking system as prose generation.

9. **Admin token usage dashboard**: Add a `/admin/tokens` page showing aggregate token usage by user, by deal, and by day. Include cost estimates based on configurable per-token rates.

## Acceptance Criteria
- ConversationHistory is sent correctly in HTTP ClaudeClient (multi-turn works)
- Chat conversations are truncated to stay within context window limits
- Send button disabled while awaiting response; 2-second cooldown enforced
- Users see "daily limit reached" when exceeding configured token budget
- Per-deal usage tracked and enforced with 80% warning and 100% block
- Every Claude API call persisted to TokenUsageRecord with full metadata
- HTTP 429 not retried; user sees friendly "busy" message
- SalesCompExtractor tokens flow through the same tracking pipeline
- Admin dashboard shows usage breakdown by user, deal, and day with cost estimates
- All existing tests pass (no regressions)
- New unit tests for truncation, budget enforcement, 429 handling, usage tracking

## Out of Scope
- Billing integration or payment processing
- Per-model pricing tiers (use single configurable rate)
- Token usage alerts via email or Slack
- Prompt caching / optimization to reduce token consumption
- Streaming responses
- User-facing usage dashboard (admin only for now)
