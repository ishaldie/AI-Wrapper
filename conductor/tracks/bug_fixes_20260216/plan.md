# Implementation Plan: Bug Fixes

## Phase 1: SSR Interactivity Fixes (Landing + Login)

- [x] Task 1.1: Write test — Landing page search bar renders as link/anchor to `/login`
- [x] Task 1.2: Fix Landing.razor — Replace `@onfocus`/`@onclick` with plain `<a href="/login">` wrappers so search bar and arrow button work in static SSR
- [x] Task 1.3: Write test — Login social buttons render "coming soon" text or noscript fallback
- [x] Task 1.4: Fix Login.razor — Replace `@onclick` on social buttons with SSR-compatible approach
- [x] Task 1.5: Verify build + all tests pass (515 pass, 0 fail)
- [x] Task 1.6: Fix Dashboard.razor search bar — wire arrow button to navigate to `/deals/new` with query
- [x] Task 1.7: Fix AnalysisStart.razor search bar — wire input + arrow button to navigate to `/deals/new` with query
- [x] Task 1.8: Verify build + all tests pass (515 pass, 0 fail)
- [x] Task 1.9: Phase 1 Manual Verification — all 4 search bars verified in Chrome

## Phase 2: Landing Redirect + Logout CSRF Fix

- [x] Task 2.1: Write test — POST /api/auth/logout endpoint returns redirect
- [x] Task 2.2: Fix Landing.razor — Move authenticated redirect from render markup to `OnInitializedAsync` lifecycle
- [x] Task 2.3: Fix Logout.razor — GET no longer signs out; POST /api/auth/logout handles sign-out
- [x] Task 2.4: Update IconSidebar — Logout link changed to POST form with antiforgery
- [x] Task 2.5: Verify build + all tests pass (516 pass, 0 fail)
- [ ] Task 2.6: Phase 2 Manual Verification

## Phase 3: Dead Code Cleanup

- [x] Task 3.1: Confirm no references to MainLayout or NavMenu remain in codebase (only CSS comment + self-refs)
- [x] Task 3.2: Remove `MainLayout.razor`, `MainLayout.razor.css`, `NavMenu.razor`, `NavMenu.razor.css`
- [x] Task 3.3: Remove legacy sidebar CSS from app.css; no test csproj changes needed
- [x] Task 3.4: Verify build + all tests pass (539 pass, 0 fail)
- [x] Task 3.5: Phase 3 complete — dead code removed, no references remain

---
