# Implementation Plan: Prompt Token Optimization

## Phase 1: System Role Consolidation

- [ ] Task: Extract shared system role suffix ("You produce institutional-quality prose... Be precise... Do not use markdown headers...") into a `BaseSystemSuffix` constant
- [ ] Task: Refactor 6 system role constants into a `BuildSystemRole(PropertyType, string focusSuffix)` method that combines property-type intro + base suffix + section focus
- [ ] Task: Update all 6 prompt methods to use the new `BuildSystemRole` method
- [ ] Task: Update existing tests to verify system roles still contain expected property-type keywords
- [ ] Task: Phase 1 Manual Verification

## Phase 2: Conciseness Instructions & max_tokens

- [ ] Task: Add explicit conciseness directive constant: "Keep each point to 2-3 sentences. No preamble or recap. Start directly with analysis."
- [ ] Task: Append conciseness directive to all 6 prompt user messages
- [ ] Task: Lower max_tokens: Executive Summary 2048→1024, Market Context 1536→1024, Value Creation 1536→1024, Risk Assessment 2048→1536, Investment Decision 1536→1024, Property Overview 512→256
- [ ] Task: Update tests that assert specific max_tokens values
- [ ] Task: Phase 2 Manual Verification

## Phase 3: Context Deduplication

- [ ] Task: Remove `AppendFinancialMetrics` from `BuildValueCreationPrompt` (only uses cap rates, already inlined)
- [ ] Task: Remove `AppendPropertyHeader` from prompts where it's redundant — keep only in Executive Summary, Risk Assessment, and Property Overview; replace with one-line "Property: {name}, {address}, {units} units" in Market Context, Value Creation, and Investment Decision
- [ ] Task: Create `AppendComplianceSummaryLine` that outputs a single line ("Fannie Mae Compliance: PASS | DSCR 1.45x vs 1.25x min | LTV 72% vs 80% max") for use in Executive Summary
- [ ] Task: Replace full `AppendFannieComplianceSection` / `AppendFreddieComplianceSection` in Executive Summary with the new one-line summary
- [ ] Task: Move `AppendSeniorHousingMetrics` and CMS data to Risk Assessment only (remove from Executive Summary and Market Context)
- [ ] Task: Update all affected tests for new prompt content structure
- [ ] Task: Phase 3 Manual Verification

---
