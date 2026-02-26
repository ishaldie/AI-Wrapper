# Spec: SOC 2 Remediation

## Overview
Address critical and medium-severity security gaps identified during SOC 2 readiness assessment. These are code-level fixes to authentication, logging, session management, security headers, and data protection — preparing the application for a SOC 2 Type II observation period. No user-facing behavior changes; all fixes are invisible hardening.

## Requirements

1. **Cryptographic OTP generation**: Replace `Random.Shared.Next()` in `EmailCodeService` with `RandomNumberGenerator.GetInt32()` for cryptographically secure 6-digit codes.

2. **OTP brute-force protection**: Add attempt counter to code validation — max 5 attempts per code, then invalidate the code and force re-request. Rate-limit the `/verify-code` and login endpoints.

3. **Remove hardcoded admin password**: Replace `Admin123!` in `SeedData.cs` with environment-variable-driven password. Log a warning if the seed password env var is missing.

4. **Redact OTP from logs**: Remove the fallback `_logger.LogInformation("Verification code for {Email}: {Code}")` that leaks the raw OTP code to log files. Log only that a code was sent, not the code itself.

5. **Security headers middleware**: Add response headers — Content-Security-Policy, X-Content-Type-Options: nosniff, X-Frame-Options: DENY, Referrer-Policy: strict-origin-when-cross-origin, Permissions-Policy (restrict camera, microphone, geolocation).

6. **Cookie hardening**: Explicitly set `SecurePolicy = Always`, `SameSite = Strict`, and add an absolute session timeout (24 hours) alongside the existing sliding expiration.

7. **ASP.NET Data Protection key persistence**: Configure `AddDataProtection()` with a persistent file-system key ring path (for dev) or Azure Key Vault (for production config).

8. **IP address in audit events**: Capture `HttpContext.Connection.RemoteIpAddress` in `ActivityTracker` and include it in `ActivityEvent` records and structured log entries.

9. **Lock AllowedHosts**: Replace `"*"` wildcard in `appsettings.json` with the production hostname. Use `appsettings.Development.json` for `localhost`.

## Acceptance Criteria
- OTP codes generated with `RandomNumberGenerator` (CSPRNG)
- OTP validation rejects after 5 failed attempts and invalidates the code
- No OTP codes appear in any log output
- Admin seed password comes from `ADMIN_SEED_PASSWORD` env var
- All 5 security headers present on every HTTP response
- Auth cookie: `Secure=Always`, `SameSite=Strict`, absolute 24-hour timeout
- Data Protection keys persisted to a configured path
- `ActivityEvent` records include client IP address
- `AllowedHosts` locked to specific hostname in production config
- All existing tests continue to pass (no regressions)
- New unit tests for OTP brute-force, security headers, cookie config

## Out of Scope
- Encryption at rest (SQLite -> SQL Server migration — separate infra track)
- Centralized log sink / SIEM integration (infrastructure/ops concern)
- MFA for admin accounts (requires TOTP/WebAuthn UI — separate track)
- Penetration testing engagement
- Written security policies and documentation
- SendGrid webhook signature verification (email ingest hardening — separate track)
- Vendor inventory and risk assessment documentation
