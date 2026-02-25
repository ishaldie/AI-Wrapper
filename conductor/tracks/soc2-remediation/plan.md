# Plan: SOC 2 Remediation

## Phase 1: Cryptographic OTP & Brute-Force Protection
- [x] 573845b 1.1 Replace `Random.Shared.Next()` with `RandomNumberGenerator.GetInt32(100000, 1000000)` in `EmailCodeService.cs`
- [x] 39b948e 1.2 Add attempt tracking to OTP validation — store attempt count alongside code in `IMemoryCache`, max 5 attempts per code
- [x] 39b948e 1.3 Invalidate OTP code after 5 failed attempts, force user to request a new code
- [x] 45d4189 1.4 Add rate limiter to `/verify-code` POST — 10 requests per 5-minute window per IP
- [x] 517511a 1.5 Redact OTP code from all log output — log "Code sent to {Email}" without the code value
- [x] 517511a 1.6 Unit tests: CSPRNG range validation, brute-force lockout after 5 attempts, rate limiter rejects excess requests, no code in log output

## Phase 2: Seed Data & Secrets Hardening
- [x] 2763742 2.1 Replace hardcoded `Admin123!` in `SeedData.cs` with `Environment.GetEnvironmentVariable("ADMIN_SEED_PASSWORD")`
- [x] 2763742 2.2 Log a warning at startup if `ADMIN_SEED_PASSWORD` is not set and skip admin seeding
- [x] 24fd76c 2.3 Add `ADMIN_SEED_PASSWORD` to `appsettings.Development.json` (dev-only) and document in README
- [x] 796df11 2.4 Lock `AllowedHosts` in `appsettings.json` to production hostname, set `localhost` in `appsettings.Development.json`
- [x] 2763742 2.5 Unit tests: seed skips when env var missing, seed creates admin when env var present

## Phase 3: Security Headers Middleware
- [x] 8f33060 3.1 Create `SecurityHeadersMiddleware` — adds CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy to every response
- [x] 8f33060 3.2 Configure CSP for Blazor Server compatibility — allow `'self'`, `'unsafe-inline'` for styles (MudBlazor), WebSocket for `_blazor`, and script hashes as needed
- [x] 8f33060 3.3 Register middleware in `Program.cs` pipeline before `UseRouting()`
- [x] 8f33060 3.4 Extend HSTS max-age to 1 year (`31536000` seconds) with `includeSubDomains`
- [x] 8f33060 3.5 Integration tests: verify all 5 headers present on responses, verify CSP does not break Blazor WebSocket

## Phase 4: Cookie & Session Hardening
- [x] 2ecf65d 4.1 Add `options.Cookie.SecurePolicy = CookieSecurePolicy.Always` to `ConfigureApplicationCookie`
- [x] 2ecf65d 4.2 Add `options.Cookie.SameSite = SameSiteMode.Strict`
- [x] 2ecf65d 4.3 Add absolute session timeout — `options.ExpireTimeSpan = TimeSpan.FromHours(24)` as hard ceiling alongside sliding expiration
- [x] 2ecf65d 4.4 Configure `AddDataProtection()` with `PersistKeysToFileSystem()` using a path from configuration, with app name isolation
- [x] 2ecf65d 4.5 Unit tests: verify cookie options are configured correctly, Data Protection key directory exists

## Phase 5: Audit Trail Enhancement
- [x] 16bb0c5 5.1 Add `IpAddress` string property to `ActivityEvent` entity
- [x] 16bb0c5 5.2 Add EF migration for `IpAddress` column on `ActivityEvents` table
- [x] 16bb0c5 5.3 Update `ActivityTracker` to accept `IHttpContextAccessor` and capture `RemoteIpAddress` on every tracked event
- [x] 16bb0c5 5.4 Register `IHttpContextAccessor` in DI if not already registered
- [x] 16bb0c5 5.5 Add IP address to structured log enrichment via Serilog `WithClientIp()` or manual `LogContext.PushProperty`
- [x] 16bb0c5 5.6 Unit tests: IP captured in activity events, null IP handled gracefully (e.g., background jobs)
