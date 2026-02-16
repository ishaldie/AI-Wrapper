# Plan: Authentication & User Management

## Phase 1: Identity Setup
- [x] Add ASP.NET Identity NuGet packages (pre-existing)
- [x] Create `ApplicationUser` entity extending `IdentityUser` in Domain (pre-existing)
- [x] Configure Identity in `Program.cs` with EF Core (pre-existing)
- [x] Create Identity database migration (pre-existing)
- [x] Seed Admin role and default admin user
- [x] Write tests for user creation and role assignment

## Phase 2: Login & Registration UI
- [ ] Create `Register.razor` page with form validation
- [ ] Create `Login.razor` page with remember-me checkbox
- [ ] Create `Logout` endpoint
- [ ] Add `[Authorize]` attribute to app routes
- [ ] Create `RedirectToLogin` component for unauthenticated users
- [ ] Write tests for registration validation and login flow

## Phase 3: Role Management
- [ ] Create `UserManagement.razor` admin page
- [ ] Display user list with roles
- [ ] Add role assignment functionality (Analyst/Admin)
- [ ] Restrict admin pages to Admin role
- [ ] Configure account lockout policy
- [ ] Write tests for role-based access control
