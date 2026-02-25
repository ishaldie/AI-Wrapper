# Plan: BYOK API Key Support

## Phase 1: Encrypted Key Storage & JSON Parsing
- [x] 1.1 Add `EncryptedAnthropicApiKey` and `PreferredModel` nullable string columns to `ApplicationUser` entity
- [x] 1.2 Add EF migration for the new columns
- [x] 1.3 Create `IApiKeyService` interface with `SaveKeyAsync(userId, apiKey, model?)`, `GetDecryptedKeyAsync(userId)`, `RemoveKeyAsync(userId)`, `HasKeyAsync(userId)`
- [x] 1.4 Implement `ApiKeyService` — encrypt via `IDataProtector` with purpose string `"AnthropicApiKey"` on save, decrypt on read
- [x] 1.5 Create `CredentialsFileParser` utility — parse JSON from string or stream, extract `api_key` field, optional `model` and `label` fields. Support both `{"api_key": "..."}` and `{"api_key": "...", "model": "...", "label": "..."}` formats
- [x] 1.6 Unit tests: encryption round-trips correctly, JSON parsing handles both formats, missing `api_key` field throws, malformed JSON throws, null/empty handling

## Phase 2: Key Resolution in ClaudeClient
- [x] 2.1 Create `IApiKeyResolver` interface with `ResolveAsync(userId?)` returning `ApiKeyResolution(apiKey, model, isByok)`
- [x] 2.2 Implement `ApiKeyResolver` — check `IApiKeyService` for user's BYOK key first, fall back to `ClaudeOptions.ResolvedApiKey`
- [x] 2.3 Update `ClaudeClient` constructor to accept `IApiKeyResolver` and resolve the key per-request instead of using the shared `_options` key
- [x] 2.4 Update `ClaudeClient` to set the `x-api-key` header per-request from the resolved key (not from a shared `HttpClient` default header)
- [x] 2.5 If user has a `PreferredModel` set, use it instead of `ClaudeOptions.Model` for that request
- [x] 2.6 Pass `userId` through the call chain: added `UserId` property to `ClaudeRequest` (cleaner than changing interface signature)
- [x] 2.7 Thread `userId` from `DealChatTab.razor`, `ReportProseGenerator`, and `SalesCompExtractor` into Claude calls
- [x] 2.8 Unit tests: BYOK key used when present, fallback to shared key when absent, preferred model override works, userId propagation

## Phase 3: Key Validation & Account Settings UI
- [x] 3.1 Add `ValidateKeyAsync(apiKey)` method to `IApiKeyService` — sends minimal request to `/v1/messages` with the provided key, returns success/failure with error message
- [x] 3.2 Create `AccountSettings.razor` page at `/account/settings` with "AI Configuration" section
- [x] 3.3 Add masked text input for API key — shows `sk-ant-****...XXXX` (last 4 chars) when key exists, full input when entering new key
- [x] 3.4 Add JSON file upload — `InputFile` component that reads the file, passes to `CredentialsFileParser`, extracts key and optional model
- [x] 3.5 Add "Test Connection" button — calls `ValidateKeyAsync`, shows Snackbar success/error
- [x] 3.6 Add "Save Key" button — validates then saves via `ApiKeyService.SaveKeyAsync`
- [x] 3.7 Add "Remove Key" button with confirmation dialog — calls `ApiKeyService.RemoveKeyAsync`
- [x] 3.8 bUnit tests: settings page renders, masked key display, remove button clears key

## Phase 4: Budget Bypass & BYOK Indicator
- [x] 4.1 Update `TokenBudgetService.CheckUserBudgetAsync` (from token-management track) to skip daily budget enforcement when `IApiKeyService.HasKeyAsync(userId)` returns true
- [x] 4.2 Add BYOK indicator component — small `MudChip` or badge in `DealChatTab.razor` header showing "Using your API key" when BYOK is active, "Using shared key" otherwise
- [x] 4.3 Token usage tracking still records all calls regardless of BYOK status — add `IsByok` bool column to `TokenUsageRecord` for cost attribution
- [x] 4.4 Add EF migration for `IsByok` column on `TokenUsageRecords`
- [x] 4.5 Unit tests: BYOK user bypasses daily budget, per-deal budget still enforced, usage recorded with IsByok flag

## Phase 5: Admin Visibility
- [ ] 5.1 Add BYOK column to admin token dashboard showing which users have their own key configured
- [ ] 5.2 Filter token usage views by BYOK vs shared key usage
- [ ] 5.3 Unit tests: admin dashboard shows BYOK status
