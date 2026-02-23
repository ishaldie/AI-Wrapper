# Plan: UI Redesign & Web Fixes

## Phase 1: Foundation - Design System & Static Asset Fix
*Status: Complete*

- [x] Task 1.1: Update App.razor - Replace Inter/Roboto with Plus Jakarta Sans font
- [x] Task 1.2: Rewrite app.css - Remove bootstrap remnants, add auth form styles, card classes, KPI icon classes, fadeInUp animations
- [x] Task 1.3: Update MainLayout.razor - Custom MudTheme (Plus Jakarta Sans, refined PaletteLight, 10px radius, 240px drawer), gradient logo mark, refined AppBar/Drawer
- [x] Task 1.4: Strip MainLayout.razor.css - Remove old template CSS, keep blazor-error-ui only
- [x] Task 1.5: Update NavMenu.razor - Bordered active state, WORKSPACE/ADMINISTRATION section headers, updated icons
- [x] Task 1.6: Empty NavMenu.razor.css - Remove old template CSS
- [x] Task 1.7: Fix Program.cs - `MapStaticAssets().AllowAnonymous()` to unblock CSS/JS for unauthenticated users

## Phase 2: Auth Pages - SSR Form Fix
*Status: Complete*

- [x] Task 2.1: Rewrite Login.razor - Replace MudTextField with HTML `<input name="Input.Email">` etc., branded card with gradient logo, custom-styled inputs
- [x] Task 2.2: Rewrite Register.razor - Same SSR-compatible HTML inputs pattern, branded card
- [x] Task 2.3: Remove duplicate `<AntiforgeryToken />` (EditForm with FormName auto-includes it)

## Phase 3: Page Redesigns
*Status: Complete*

- [x] Task 3.1: Dashboard.razor - Welcome header with AuthorizeView, KPI cards with colored left-borders and animated fade-in, recent activity section
- [x] Task 3.2: DealPipeline.razor - Page header, bordered MudPaper wrapper, Elevation="0" on DataGrid
- [x] Task 3.3: UserManagement.razor - Page header, Elevation="0" MudTable with border
- [x] Task 3.4: ~~DealWizard.razor~~ - Removed (replaced by AnalysisStart + DealChat flow)
- [x] Task 3.5: DealCard.razor - Elevation="0" with border, muted labels
- [x] Task 3.6: Error.razor - Rewritten with MudBlazor components
- [x] Task 3.7: NotFound.razor - Rewritten with MudBlazor components
- [x] Task 3.8: ReportViewer.razor - All Elevation="1"/"2" changed to Elevation="0" with borders

## Phase 4: Test Compatibility
*Status: Complete*

- [x] Task 4.1: Update DashboardTests.cs - Add `_ctx.AddAuthorization()` + `SetAuthorized("Test User")` for AuthorizeView
- [x] Task 4.2: Update test assertions - "Welcome back" heading, "underwriting pipeline" text
- [x] Task 4.3: Verify 498+ tests pass (excluding 4 pre-existing WebApplicationFactory intermittent failures)

## Phase 5: Site Walkthrough & Bug Fixes
*Status: Pending*

### 5A: Public pages (PublicLayout, unauthenticated)
- [~] Task 5.1: Review Landing page - "UA" logo, "Underwriting Analyst" brand text, nav links, hero section
- [ ] Task 5.2: Review Login page - branded card, SSR form submission, social login buttons, validation
- [ ] Task 5.3: Review Register page - branded card, SSR form submission, validation display
- [ ] Task 5.4: Review VerifyCode page - branded card, code input, resend flow
- [ ] Task 5.5: Review About page - heading, body copy, "Underwriting Analyst" references (not "ZSR Underwriting")
- [ ] Task 5.6: Review Terms & Privacy pages - legal content renders, back link works
- [ ] Task 5.7: Review Error & NotFound pages - navigate to /not-found, verify styling

### 5B: Auth gate
- [ ] Task 5.8: Review AcceptTerms page - "Underwriting Analyst" branding, checkbox, submit flow

### 5C: Authenticated pages (AppLayout + IconSidebar)
- [ ] Task 5.9: Review IconSidebar - "UA" logo, nav icons, active states, logout button
- [ ] Task 5.10: Review Dashboard page - KPI cards, animations, recent activity, layout at 100% zoom
- [ ] Task 5.11: Review AnalysisStart page - form inputs, file upload, quick analysis flow
- [ ] Task 5.12: Review DealPipeline page - DataGrid, filters, search, empty state
- [ ] Task 5.13: Review DealChat page - chat UI, message rendering, document upload
- [ ] Task 5.14: Review DealReport page - report sections, navigation
- [ ] Task 5.15: Review QuickAnalysisPage - progress steps, loading states
- [ ] Task 5.16: Review QuickReport page - report display, data formatting
- [ ] Task 5.17: Review DealComparison page - side-by-side layout, data display
- [ ] Task 5.18: Review DealCard component - elevation, borders, labels
- [ ] Task 5.19: Review ReportViewer component - sections, elevation, borders

### 5D: Admin
- [ ] Task 5.20: Review User Management page (admin) - table, role chips, actions

### 5E: Wrap-up
- [ ] Task 5.21: Fix any visual or functional issues discovered during walkthrough
- [ ] Task 5.22: Final build + test verification
- [ ] Task 5.23: Commit all changes
