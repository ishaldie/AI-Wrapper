# Spec: Activity Tracking Completion

## Overview
Wire up the 9 defined-but-never-emitted ActivityEventType values to their corresponding call sites, add missing tracking for document downloads and OAuth logins, fix the PdfExported event to include DealId, and build an admin activity dashboard for reviewing user sessions and events.

## Requirements

1. **Wire DealCreated event**: Emit `DealCreated` from `QuickAnalysisService` and any other code path that creates a deal, passing the new `DealId`.

2. **Wire DocumentUploaded event**: Emit `DocumentUploaded` from the file upload handler in `DealTabs.razor` with `DealId` and filename as metadata.

3. **Wire DocumentDeleted event**: Emit `DocumentDeleted` from the document delete action in `DealTabs.razor` or `DocumentEndpoints` with `DealId` and filename.

4. **Wire DocumentAccessDenied event**: Emit `DocumentAccessDenied` from `DocumentUploadService.VerifyDealOwnershipAsync` when access is denied, with the attempted `DealId` and userId.

5. **Wire DocumentScanFailed event**: Emit `DocumentScanFailed` from `DocumentUploadService` when a virus scan fails, with `DealId` and filename.

6. **Wire DocumentRateLimited event**: Emit `DocumentRateLimited` when the upload rate limiter returns 429, capturing the userId.

7. **Wire WizardStarted / WizardCompleted events**: Emit from the quick analysis flow entry and completion points.

8. **Add document download tracking**: Emit a new `DocumentDownloaded` event type from `/api/documents/{id}/download` endpoint with `DealId` and filename.

9. **Add OAuth login tracking**: Emit a new `OAuthLoginCompleted` event type from `ExternalAuthEndpoints.cs` callback with the provider name as metadata.

10. **Fix PdfExported event**: Pass `DealId` alongside `PropertyName` metadata in `ReportViewer.razor` so exports can be correlated to deals.

11. **Admin Activity Dashboard**: Build an admin-only page at `/admin/activity` showing recent sessions and events with filtering by user, event type, date range, and deal. Use MudDataGrid for the event log table.

## Acceptance Criteria
- All 17 original ActivityEventType values are emitted from at least one call site
- Two new event types added: DocumentDownloaded, OAuthLoginCompleted
- PdfExported events include DealId
- Admin dashboard at `/admin/activity` displays sessions and events
- Dashboard supports filtering by user, event type, date range, and deal
- Admin-only access enforced via `[Authorize(Roles = "Admin")]`
- All existing tests pass (no regressions)
- New unit tests for each newly wired event emission
- bUnit test for admin dashboard rendering

## Out of Scope
- Real-time event streaming / live dashboard updates
- Event export to CSV/Excel
- Event retention/archival policies
- Alerting or notification on specific event types
- Analytics aggregation (charts, trends, metrics)
