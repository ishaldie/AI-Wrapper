# CGX: Claude AI Report Generation

## Frustrations & Friction
- [2026-02-15] Test csproj whitelist pattern requires manual Compile Include for every new test file — easy to forget
- [2026-02-15] MockHttpMessageHandler was duplicated because original is in excluded RealAi directory — extracted to shared Helpers
- [2026-02-15] Deal entity has both `Name` (private set) and `PropertyName` (public set) — confusing, tests set wrong one initially

## Good Patterns
- [2026-02-15] TDD cycle clean: write tests → confirm Red → implement → confirm Green → regression check
- [2026-02-15] Sequential section generation in ReportProseGenerator avoids Claude API rate limits
- [2026-02-15] Partial failure pattern (catch per-section, record failures) preserves successful sections

## Anti-Patterns

## Missing Capabilities
- [2026-02-15] No structured output parsing from Claude responses yet (highlights, risks, conditions extracted as lists)

## Improvement Candidates
- [2026-02-15] Future: parse Claude JSON-structured responses for KeyHighlights, KeyRisks, Conditions, NextSteps lists
- [2026-02-15] Future: parallel section generation with rate-limiting for faster report generation
