# Plan: Authentication & User Management

## Phase 1: Identity Setup
- [x] Add ASP.NET Identity NuGet packages (pre-existing)
- [x] Create `ApplicationUser` entity extending `IdentityUser` in Domain (pre-existing)
- [x] Configure Identity in `Program.cs` with EF Core (pre-existing)
- [x] Create Identity database migration (pre-existing)
- [x] Seed Admin role and default admin user
- [x] Write tests for user creation and role assignment

## Phase 2: Login & Registration UI
- [x] Create `Register.razor` page with form validation
- [x] Create `Login.razor` page with remember-me checkbox
- [x] Create `Logout` endpoint
- [x] Add `[Authorize]` attribute to app routes (fallback policy)
- [x] Create `RedirectToLogin` component for unauthenticated users
- [x] Write tests for registration validation and login flow

## Phase 3: Role Management
- [x] Create `UserManagement.razor` admin page
- [x] Display user list with roles
- [x] Add role assignment functionality (Analyst/Admin)
- [x] Restrict admin pages to Admin role (`[Authorize(Roles = "Admin")]`)
- [x] Configure account lockout policy (pre-existing: 5 attempts, 15 min)
- [x] Write tests for role-based access control
