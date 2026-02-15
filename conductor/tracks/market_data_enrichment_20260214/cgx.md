# CGX: Market Data Enrichment

## Frustrations & Friction
- [2026-02-15] Post-commit hook rewrites test csproj whitelist on every commit, removing our track's test includes and re-adding broken includes from other tracks. Required re-adding market data test entries after each commit.
- [2026-02-15] Multiple tracks have uncommitted/partial files in working directory (DTOs/Report, FileUpload.razor, DocumentUploadServiceTests) that break the build. Had to add Application csproj exclusions for `DTOs\Report\**`.

## Good Patterns
- [2026-02-15] MockHttpMessageHandler pattern in WebSearchServiceTests works cleanly for testing HttpClient-based services without external dependencies.
- [2026-02-15] Static query builder (MarketSearchQueryBuilder) keeps query logic pure and easily testable.

## Anti-Patterns
- [2026-02-15] Task 1 was already committed by a prior session but plan.md wasn't updated — caused confusion about what work was actually needed.

## Missing Capabilities

## Improvement Candidates
- [2026-02-15] The csproj hook should preserve existing track test includes rather than resetting to its own list. Consider a merge strategy instead of overwrite.
