# Implementation Plan: Terms of Service & Privacy Policy

## Phase 1: Domain Model & Database

- [x] Task 1.1: Add `TosAcceptedAt` (DateTime?) and `TosVersion` (string?) properties to `ApplicationUser` entity
- [x] Task 1.2: Add `TosVersion` setting to `appsettings.json` (value: `"1.0"`)
- [x] Task 1.3: Schema update via EnsureCreated (project uses EnsureCreated, not migrations — delete dev SQLite DB to pick up new columns)
- [x] Task 1.4: Phase 1 Manual Verification — 577/577 tests passing, stale DBs cleaned

---

## Phase 2: Public Legal Pages

- [x] Task 2.1: Create `/terms` page — static SSR, `[AllowAnonymous]`, `PublicLayout`, full TOS content (15 sections adapted from RealAI)
- [x] Task 2.2: Create `/privacy` page — static SSR, `[AllowAnonymous]`, `PublicLayout`, full Privacy Policy content (10 sections adapted from RealAI)
- [x] Task 2.3: Add footer links to Terms of Service and Privacy Policy on `PublicLayout`
- [x] Task 2.4: Add footer link to Terms of Service and Privacy Policy in `AppLayout`
- [x] Task 2.5: Phase 2 Manual Verification — 585/585 tests passing, pages accessible without auth

---

## Phase 3: Accept Terms Gate Page

- [x] Task 3.1: Create `/accept-terms` page — static SSR, `[Authorize]`, `PublicLayout`, checkbox + submit form
- [x] Task 3.2: Implement form POST handler — validate checkbox, update `ApplicationUser.TosAcceptedAt` and `TosVersion`, redirect to `/search`
- [x] Task 3.3: Phase 3 Manual Verification — 9/9 legal page tests passing, redirect works for unauthenticated users

---

## Phase 4: Registration & OAuth Integration

- [ ] Task 4.1: Add TOS/Privacy checkbox to Register page with validation (form fails if unchecked)
- [ ] Task 4.2: Record `TosAcceptedAt` and `TosVersion` on user entity during registration submit
- [ ] Task 4.3: Modify `ExternalAuthEndpoints.HandleExternalCallback` — redirect new OAuth users to `/accept-terms` instead of `/search`
- [ ] Task 4.4: Phase 4 Manual Verification — test registration with/without checkbox, test new Google/Microsoft OAuth user redirect

---

## Phase 5: TOS Enforcement Middleware

- [ ] Task 5.1: Create TOS enforcement middleware — check authenticated user's `TosVersion` against config, redirect to `/accept-terms` if mismatch
- [ ] Task 5.2: Configure exempt paths: `/accept-terms`, `/terms`, `/privacy`, `/logout`, `/api/auth/*`, static assets
- [ ] Task 5.3: Register middleware in `Program.cs` pipeline (after `UseAuthentication`/`UseAuthorization`)
- [ ] Task 5.4: Phase 5 Manual Verification — test with matching TOS version (no redirect), bump version in config (forces redirect), verify exempt paths are not redirected

---
