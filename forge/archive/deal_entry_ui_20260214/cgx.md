# CGX: Deal Entry UI

## Frustrations & Friction
- [2026-02-15] bUnit v2.5 API changed from `TestContext`/`RenderComponent` to `BunitContext`/`Render` - caused test discovery issues
- [2026-02-15] MudBlazor components require `JSInterop.Mode = JSRuntimeMode.Loose` in bUnit tests
- [2026-02-15] `IDisposable` not enough for bUnit with MudBlazor - need `IAsyncLifetime` due to `IAsyncDisposable` services
- [2026-02-15] MudBlazor v8 removed `Linear`, `Color`, `Icon` params from `MudStepper`/`MudStep` - analyzer caught these

## Good Patterns
- [2026-02-15] Extracting step components into a DealEntry/ subfolder keeps Pages clean
- [2026-02-15] FluentValidation `When()` clauses for optional fields - only validate when values are provided

## Anti-Patterns

## Missing Capabilities

## Improvement Candidates
