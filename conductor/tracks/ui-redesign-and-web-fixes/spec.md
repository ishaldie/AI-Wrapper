# Spec: UI Redesign & Web Fixes

**Track ID:** `ui-redesign-and-web-fixes`
**Type:** refactor
**Created:** 2026-02-16

## Overview
Redesign the ZSR Underwriting web UI with a distinctive, production-grade aesthetic following frontend-design skill principles (Plus Jakarta Sans typography, refined color system, CSS animations), fix SSR form binding bugs on auth pages, and resolve any remaining runtime errors across the website.

## Requirements
1. **SSR Form Fix** - Login and Register pages use standard HTML `<input>` elements with proper `name` attributes for static SSR form binding (MudBlazor's MudTextField doesn't render names in SSR mode)
2. **Typography Upgrade** - Replace Inter/Roboto with Plus Jakarta Sans (distinctive, geometric, professional)
3. **Theme Refinement** - Updated MudTheme with tighter letter-spacing, bolder heading weights (800), refined color palette (#F0F2F5 background, #1A1D23 text primary), 10px border radius
4. **Card System** - `.card-elevated` class with hover shadow transitions, `.card-interactive` with translateY micro-interaction
5. **Animation System** - Staggered `fadeInUp` on page load (`.animate-in .delay-1` through `.delay-5`)
6. **Auth Page Polish** - Branded login/register cards with gradient logo mark, custom-styled HTML form inputs
7. **KPI Dashboard Icons** - Rounded-square icons (12px radius) with gradient fills replacing plain circles
8. **Static Asset Auth Fix** - `MapStaticAssets().AllowAnonymous()` to prevent CSS/JS blocking for unauthenticated users
9. **Full Site Walkthrough** - Navigate every page in the app, identify and fix any remaining visual or functional issues
10. **Test Compatibility** - All existing bUnit/xUnit tests pass with UI changes

## Acceptance Criteria
- [ ] Login form submits successfully with email/password (SSR form binding works)
- [ ] Register form creates new accounts (SSR form binding works)
- [ ] All pages use Plus Jakarta Sans typography
- [ ] Dashboard shows staggered fade-in animation on load
- [ ] KPI cards have colored left-border accents and hover transitions
- [ ] Auth pages render custom-styled inputs (not MudBlazor MudTextField)
- [ ] MudBlazor CSS loads for unauthenticated users
- [ ] `dotnet build` passes with zero errors
- [ ] 494+ tests pass (excluding pre-existing WebApplicationFactory intermittent failures)
- [ ] All pages visually reviewed and free of layout/rendering issues

## Out of Scope
- Dark mode theme
- Mobile-responsive redesign
- New page creation or feature additions
- Backend logic changes
