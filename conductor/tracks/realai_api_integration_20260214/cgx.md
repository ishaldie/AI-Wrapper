# CGX: RealAI API Integration

## Frustrations & Friction
- [2026-02-15] A hook/linter auto-manages csproj Compile Remove entries and deletes test files placed in Infrastructure/ for "unimplemented" tracks. Workaround: place track tests in dedicated `RealAi/` subdirectory under tests.
- [2026-02-15] Pre-existing `RealAiTypes.cs` in Domain/ValueObjects conflicted with new per-type files — had to delete the combined file.
- [2026-02-15] Dual conductor directories (App/conductor/ and AI Wrappers/conductor/) — must update both.

## Good Patterns

## Anti-Patterns

## Missing Capabilities

## Improvement Candidates
