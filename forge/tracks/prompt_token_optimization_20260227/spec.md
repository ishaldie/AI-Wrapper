# Spec: Prompt Token Optimization

## Overview
Reduce Claude API token usage in underwriting report generation by 25-40% through prompt engineering — consolidating redundant system roles, deduplicating repeated context blocks, lowering max_tokens caps, and adding explicit conciseness instructions.

## Problem
The report generator makes 6 sequential Claude API calls, each rebuilding the same context independently. Property headers are sent 5 times, financial metrics 4 times, compliance sections 3 times, and 6 nearly-identical system roles each carry ~80 tokens of shared boilerplate. max_tokens values are generous (9,216 total) when tighter caps would produce equally good output.

## Requirements
1. Consolidate 6 system role constants into a shared base + property-type specialization
2. Remove financial metrics from prompts that don't use them (Value Creation, Property Overview)
3. Use one-line compliance summary in Executive Summary; full detail only in Risk Assessment and Investment Decision
4. Include CMS data only in Risk Assessment (not Executive Summary or Market Context)
5. Lower max_tokens per section to tighter realistic caps
6. Add explicit conciseness instructions in every prompt ("2-3 sentences per point, no preamble")
7. All existing tests must continue to pass
8. No change to report quality — output should be equally analytical, just less verbose

## Technical Approach
- Modify `UnderwritingPromptBuilder.cs` — refactor system roles, add conciseness directives, dedup context
- Update existing tests in `PromptBuilderTests.cs` and `FanniePromptBuilderTests.cs`
- No database changes, no new services, no API changes

## Acceptance Criteria
- All 1,659+ existing tests pass
- System role strings are not duplicated across constants
- Financial metrics appear only in prompts that reference them in instructions
- Full compliance detail only in Risk Assessment and Investment Decision
- max_tokens total across all 6 sections <= 6,400 (down from 9,216)

## Out of Scope
- Batching multiple sections into a single API call (architecture change)
- Prompt caching (requires API-level changes)
- Changing report section structure or content expectations
