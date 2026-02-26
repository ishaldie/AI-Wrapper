# CGX: Underwriting Calculation Engine

## Frustrations & Friction
- [2026-02-15] OneDrive sync constantly reverts .csproj edits and creates/deletes source files mid-build
- [2026-02-15] Test project whitelist csproj approach requires manual includes for each new test file
- [2026-02-15] Multiple incomplete tracks left broken test stubs that block compilation of the entire solution
- [2026-02-15] Application csproj had `Compile Remove="Calculations\**"` from another session that silently excluded new code

## Good Patterns
- [2026-02-15] Pure function interface design (IUnderwritingCalculator) makes TDD straightforward
- [2026-02-15] Occupancy as percentage (93%) rather than decimal (0.93) matches protocol conventions

## Anti-Patterns
- [2026-02-15] Manual calculation of expected test values led to rounding errors; should compute chain values programmatically

## Missing Capabilities
- [2026-02-15] No way to lock files from OneDrive sync during active development sessions

## Improvement Candidates
- [2026-02-15] Consider .gitignore or separate non-synced working directory for active development
- [2026-02-15] Infrastructure csproj needs same whitelist approach as test csproj to prevent sync-created files from breaking build
