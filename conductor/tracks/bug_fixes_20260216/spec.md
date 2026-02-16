# Spec: Bug Fixes

**Track ID:** `bug_fixes_20260216`
**Type:** bugfix

## Overview
Fix SSR interactivity bugs on public pages (Landing, Login), resolve unsafe patterns (render-time navigation, GET-based logout), and clean up dead layout code left over from the UI redesign track.

## Requirements

1. **Landing page SSR fix** — Replace broken `@onclick`/`@onfocus` event handlers with plain HTML links/navigation since the page is static SSR (no `@rendermode`)
2. **Login social button SSR fix** — Replace broken `@onclick` handlers on Google/Microsoft buttons with a mechanism that works in static SSR (e.g., form post or JS-free approach)
3. **Landing page redirect fix** — Move the authenticated user redirect from render markup to `OnInitialized` lifecycle method to avoid side-effects during rendering
4. **Logout CSRF fix** — Change logout from GET-triggered `OnInitializedAsync` to POST-based form submission with antiforgery protection
5. **Dead code cleanup** — Remove unused `MainLayout.razor`, `MainLayout.razor.css`, and `NavMenu.razor`/`NavMenu.razor.css` since `AppLayout` is now the default layout

## Acceptance Criteria

- [ ] Landing page search bar and arrow button navigate to `/login` when clicked (without requiring InteractiveServer)
- [ ] Login social buttons show "coming soon" feedback or redirect appropriately in SSR
- [ ] Authenticated users hitting `/` redirect to `/search` without render-time side effects
- [ ] Logout requires POST (not GET), preventing CSRF-based signout
- [ ] `MainLayout.razor`, `MainLayout.razor.css`, `NavMenu.razor`, `NavMenu.razor.css` removed
- [ ] `dotnet build` passes with zero errors
- [ ] All 509+ tests pass

## Out of Scope

- Implementing actual Google/Microsoft SSO
- Implementing actual email sending for verification codes
- Mobile responsive fixes
- New feature work
