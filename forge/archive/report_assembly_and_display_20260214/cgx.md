# CGX: Report Assembly & Display

## Frustrations & Friction
- [2026-02-15] Hook persistently re-adds `<Compile Remove="Services\ReportAssembler.cs" />` to Infrastructure.csproj after every tool call, breaking builds. Requires repeated manual fixes.
- [2026-02-15] Hook removes Report track test includes (`DTOs\ReportDtoTests.cs`, `Services\ReportAssemblerTests.cs`) from test csproj whitelist, preventing tests from running.
- [2026-02-15] Hook reverts plan.md progress markers back to `[ ]` after commits, losing track of completed work.
- [2026-02-15] Hook deleted `ProtocolDefaults.cs` from Application/Constants/ directory (had to recreate).
- [2026-02-15] Hook deleted Report DTO files from `DTOs/Report/` directory between Write tool calls; had to use Bash `cat` workaround.

## Good Patterns
- [2026-02-15] Building formatting helpers first (Task 4) before DTOs/assembler was the right order - reusable across all sections.
- [2026-02-15] InMemory EF Core for assembler integration tests works well and runs fast (~800ms for all 10 tests).

## Anti-Patterns
- [2026-02-15] Hook's whitelist enforcement based on tracks.md status creates a circular problem: can't mark track in-progress in the same commit as adding files.

## Missing Capabilities

## Improvement Candidates
- [2026-02-15] Hook should respect track `[~]` status and NOT exclude files for in-progress tracks from csproj.
- [2026-02-15] Hook should not revert plan.md progress markers that were set by the implementation agent.
