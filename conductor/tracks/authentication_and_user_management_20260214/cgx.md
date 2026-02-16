# CGX: Authentication & User Management

## Frustrations & Friction
- [2026-02-15] InMemory DB gotcha: `Guid.NewGuid()` inside `AddDbContext` lambda creates a NEW database per scope resolution. Must capture the name outside the lambda closure.

## Good Patterns
- [2026-02-15] Static SSR for auth pages (Login/Register/Logout) works well — no InteractiveServer needed, cookies set correctly.
- [2026-02-15] Fallback auth policy is cleaner than adding `[Authorize]` to every page — only need `[AllowAnonymous]` on exceptions.
- [2026-02-15] `[SupplyParameterFromForm]` nullable pattern with `CurrentInput => Input ??= new()` avoids BL0008 warning.

## Anti-Patterns

## Missing Capabilities

## Improvement Candidates
