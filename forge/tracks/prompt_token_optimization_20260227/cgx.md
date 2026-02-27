# CGX: Prompt Token Optimization

## Learnings
- [2026-02-27] good-pattern: Characterization tests before refactoring (Phase 1) caught the exact same behavior pre/post change — no regressions
- [2026-02-27] good-pattern: Adding `includeCms` parameter to existing method is cleaner than splitting into two methods
- [2026-02-27] anti-pattern: Securitization comps track test files got pulled into commits via hooks — need to be aware of cross-track file staging

## Observations
- [2026-02-27] The `AppendCompactPropertyLine` pattern could be reused for other prompt optimization work
- [2026-02-27] Total max_tokens went from 9,216 to 4,888 (47% reduction) — well under the 6,400 target
