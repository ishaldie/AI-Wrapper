# Plan: Document Upload Security Hardening

## Phase 1 — Multi-Tenant Access Control

- [x] bdb5f94 Add `UserId` property (string FK to ApplicationUser) on `Deal` entity in `Deal.cs`
- [x] 010a372 Add EF migration for `UserId` column on Deals table with index
- [x] 010a372 Update `AppDbContext.OnModelCreating` to configure Deal → ApplicationUser relationship
- [x] c6e05fb Update `IDealRepository` interface — add userId parameter to `GetByIdAsync`, `GetAllAsync`, `GetByStatusAsync`
- [x] c6e05fb Update `DealRepository` — filter all queries by userId; reject access if userId doesn't match
- [x] 70e553a Update `DocumentUploadService` — verify deal ownership before upload, get, and delete operations
- [x] 70e553a Update Blazor components (`FileUpload.razor`, `Dashboard.razor`, `AnalysisStart.razor`) to pass authenticated userId from `AuthenticationStateProvider`
- [x] 70e553a Write tests: user A cannot access user B's deal; unauthorized upload returns error

## Phase 2 — File Validation & Sanitization

- [x] c379260 Add magic byte signatures to `FileUploadConstants` for PDF, XLSX, CSV, DOCX
- [x] c379260 Create `IFileContentValidator` interface in Application layer with `ValidateAsync(Stream, string extension)` method
- [x] c379260 Implement `FileContentValidator` in Infrastructure — read first N bytes and compare against magic byte map
- [x] c379260 Add MIME type allowlist to `FileUploadConstants`; validate Content-Type in `DocumentUploadService`
- [x] c379260 Sanitize filename with `Path.GetFileName()` in `DocumentUploadService.UploadDocumentAsync` before any use
- [x] c379260 Add formula injection sanitization in `RentRollParser` — strip leading `=`, `+`, `-`, `@` from cell values
- [x] c379260 Add formula injection sanitization in `T12Parser` — same pattern
- [x] c379260 Wire `IFileContentValidator` into `DocumentUploadService` — reject mismatched files before storage
- [x] c379260 Write tests: mismatched content/extension rejected; path traversal filenames sanitized; formula-prefixed cells stripped

## Phase 3 — Rate Limiting & Malware Scanning

- [x] bf571da Add ASP.NET `RateLimiter` middleware in `Program.cs` — per-user fixed window policy (10 uploads per 5 minutes)
- [x] bf571da Create `IVirusScanService` interface in Application layer with `ScanAsync(Stream)` returning scan result DTO
- [x] bf571da Implement `WindowsDefenderScanService` in Infrastructure using AMSI or MpCmdRun.exe
- [x] bf571da Add `VirusScanStatus` enum (Pending, Clean, Infected, ScanFailed) to Domain
- [x] bf571da Add `VirusScanStatus` and `FileHash` (string, SHA-256) properties to `UploadedDocument` entity
- [x] bf571da Add EF migration for new UploadedDocument columns
- [x] bf571da Wire virus scan into `DocumentUploadService` — scan before persist, store result, reject infected
- [x] bf571da Compute SHA-256 hash in `DocumentUploadService` before saving; store on entity
- [x] bf571da Write tests: rate limit returns 429; infected file rejected; hash computed and stored correctly

## Phase 4 — Audit Logging & Storage Hardening

- [~] Add security event types to `ActivityEventType`: `DocumentAccessDenied`, `DocumentScanFailed`, `DocumentRateLimited`, `DocumentDeleted`
- [~] Add `UploadedByUserId` property to `UploadedDocument` entity; add EF migration
- [~] Capture client IP in upload flow via `IHttpContextAccessor` and log with ActivityTracker
- [~] Add structured Serilog logging in `DocumentUploadService` for all security events (upload, delete, scan result, access denied)
- [~] Ensure uploads directory is not served by static files middleware — verify no `UseStaticFiles` maps to uploads path
- [~] Add integration test: direct HTTP request to `/uploads/...` returns 404
- [~] Write tests: security events logged with correct userId, IP, and event type
