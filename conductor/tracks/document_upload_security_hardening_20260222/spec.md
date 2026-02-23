# Spec: Document Upload Security Hardening

## Overview

The document upload pipeline in ZSR Underwriting has critical security gaps discovered during audit. The most severe is the lack of multi-tenant isolation — any authenticated user can access any deal and its documents. Additional gaps include extension-only file validation (no content verification), path traversal risk, CSV/XLSX formula injection, no malware scanning, no rate limiting, and minimal audit logging.

This track hardens the entire document upload and access pipeline to production-grade security.

## Requirements

1. **Multi-tenant access control**: Deals must be owned by users. All queries must filter by the authenticated user's ID. No user can access another user's deals or documents.
2. **File content validation**: Validate file content (magic bytes) matches the declared extension. Reject mismatches.
3. **Filename sanitization**: Strip path components from uploaded filenames to prevent path traversal.
4. **Formula injection protection**: Sanitize CSV/XLSX cell values to prevent formula injection (e.g., `=CMD|...`).
5. **MIME type validation**: Validate Content-Type against an allowlist in addition to extension checks.
6. **Malware scanning**: Integrate virus scanning (Windows Defender / ClamAV) before persisting uploaded files. Track scan status on documents.
7. **Rate limiting**: Per-user upload rate limits to prevent abuse and storage exhaustion.
8. **File integrity**: Store SHA-256 hash of uploaded files for integrity verification.
9. **Audit logging**: Log all file operations (upload, access, delete, scan results) with user ID, IP, and timestamp via Serilog structured events.
10. **Storage hardening**: Ensure uploads directory is not directly web-accessible. Add `UploadedByUserId` tracking on documents.

## Technical Approach

- Add `UserId` FK on `Deal` entity pointing to `ApplicationUser`
- Add `UploadedByUserId`, `FileHash`, `VirusScanStatus` fields on `UploadedDocument`
- Create `IFileContentValidator` / `FileContentValidator` in Application/Infrastructure layers
- Create `IVirusScanService` / `WindowsDefenderScanService` in Infrastructure
- Update `DealRepository` to accept and filter by userId on all queries
- Update `DocumentUploadService` to validate ownership, scan files, compute hashes
- Add `RateLimiter` middleware in Program.cs pipeline
- Sanitize parsed CSV/XLSX values in `RentRollParser` and `T12Parser`
- Extend `ActivityTracker` with security-specific event types

## Acceptance Criteria

- [ ] User A cannot see, access, or modify User B's deals or documents
- [ ] Uploading a `.csv` file that is actually a PNG is rejected
- [ ] Filenames like `../../etc/passwd.csv` are sanitized to `passwd.csv`
- [ ] CSV files containing `=CMD|` formulas have values sanitized before parsing
- [ ] Uploaded files are scanned; infected files are rejected with user notification
- [ ] A user uploading more than 10 files in 5 minutes is rate-limited
- [ ] All upload/delete/access events are logged with userId, IP, timestamp, and outcome
- [ ] SHA-256 hash is stored for every uploaded document
- [ ] Uploads directory returns 404 if accessed directly via HTTP

## Out of Scope

- Encryption at rest (can be a follow-up track)
- File versioning / soft-delete (can be a follow-up track)
- Content-based document classification
- Per-user storage quotas (beyond rate limiting)
