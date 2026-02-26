# Spec: Authentication & User Management

## Overview
Implement ASP.NET Identity with cookie-based authentication, user registration/login, and role-based access control (Analyst and Admin roles).

## Requirements
1. Configure ASP.NET Identity with EF Core
2. Create registration page with email, password, name fields
3. Create login page with remember-me option
4. Implement logout functionality
5. Define roles: Analyst (default), Admin
6. Protect all application routes behind authentication
7. Admin role can manage users (view list, assign roles)
8. Password requirements: 8+ chars, uppercase, lowercase, number
9. Account lockout after 5 failed attempts

## Acceptance Criteria
- [ ] New users can register and are assigned Analyst role
- [ ] Users can log in with email/password
- [ ] Unauthenticated users are redirected to login
- [ ] Admin users can view user list and change roles
- [ ] Password validation enforces requirements
- [ ] Account locks after 5 failed login attempts
- [ ] Logout clears session and redirects to login

## Out of Scope
- OAuth/social login providers
- Email verification / password reset (can add later)
- Multi-factor authentication
