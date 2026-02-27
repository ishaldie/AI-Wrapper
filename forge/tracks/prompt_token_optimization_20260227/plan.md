# Implementation Plan: Prompt Token Optimization

## Phase 1: System Role Consolidation [checkpoint: 5e9c8d1]

- [x] Task: Extract shared system role suffix ("You produce institutional-quality prose... Be precise... Do not use markdown headers...") into a `BaseSystemSuffix` constant 5e9c8d1
- [x] Task: Refactor 6 system role constants into a `BuildSystemRole(PropertyType, string focusSuffix)` method that combines property-type intro + base suffix + section focus 5e9c8d1
- [x] Task: Update all 6 prompt methods to use the new `BuildSystemRole` method 5e9c8d1
- [x] Task: Update existing tests to verify system roles still contain expected property-type keywords 5e9c8d1
- [x] Task: Phase 1 Manual Verification 5e9c8d1

## Phase 2: Conciseness Instructions & max_tokens [checkpoint: af17026]

- [x] Task: Add explicit conciseness directive constant: "Keep each point to 2-3 sentences. No preamble or recap. Start directly with analysis." af17026
- [x] Task: Append conciseness directive to all 6 prompt user messages af17026
- [x] Task: Lower max_tokens: Executive Summary 2048→1024, Market Context 1536→1024, Value Creation 1536→1024, Risk Assessment 2048→1536, Investment Decision 1536→1024, Property Overview 512→256 af17026
- [x] Task: Update tests that assert specific max_tokens values af17026
- [x] Task: Phase 2 Manual Verification af17026

## Phase 3: Context Deduplication [checkpoint: a189bc6]

- [x] Task: Remove `AppendFinancialMetrics` from `BuildValueCreationPrompt` (only uses cap rates, already inlined) a189bc6
- [x] Task: Remove `AppendPropertyHeader` from prompts where it's redundant — keep only in Executive Summary, Risk Assessment, and Property Overview; replace with one-line "Property: {name}, {address}, {units} units" in Market Context, Value Creation, and Investment Decision a189bc6
- [x] Task: Create `AppendComplianceSummaryLine` that outputs a single line ("Fannie Mae Compliance: PASS | DSCR 1.45x vs 1.25x min | LTV 72% vs 80% max") for use in Executive Summary a189bc6
- [x] Task: Replace full `AppendFannieComplianceSection` / `AppendFreddieComplianceSection` in Executive Summary with the new one-line summary a189bc6
- [x] Task: Move `AppendSeniorHousingMetrics` and CMS data to Risk Assessment only (remove from Executive Summary and Market Context) a189bc6
- [x] Task: Update all affected tests for new prompt content structure a189bc6
- [x] Task: Phase 3 Manual Verification a189bc6

---
