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
- [x] Task 3.4: DealWizard.razor - Added subtitle
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

- [ ] Task 5.1: Verify login flow - Log in with admin@zsr.com, confirm redirect to dashboard
- [ ] Task 5.2: Review Dashboard page - KPI cards, animations, layout at 100% zoom
- [ ] Task 5.3: Review Deal Pipeline page - DataGrid, filters, search, empty state
- [ ] Task 5.4: Review New Deal (DealWizard) page - Stepper, form inputs
- [ ] Task 5.5: Review Deal Edit/Card page - Data display, overrides
- [ ] Task 5.6: Review Report Viewer page - Sections, PDF export button
- [ ] Task 5.7: Review User Management page (admin) - Table, role chips
- [ ] Task 5.8: Review Register page - Form submission, validation display
- [ ] Task 5.9: Review Error & NotFound pages - Navigate to /not-found, verify styling
- [ ] Task 5.10: Fix any visual or functional issues discovered during walkthrough
- [ ] Task 5.11: Final build + test verification
- [ ] Task 5.12: Commit all changes
