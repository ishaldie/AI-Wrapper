# Plan: Activity Tracking Completion

## Phase 1: Wire Deal & Document Events
- [x] 1.1 Inject `IActivityTracker` into `QuickAnalysisService` and emit `DealCreated` after deal creation with `DealId`
- [x] 1.2 Inject `IActivityTracker` into `DealTabs.razor` and emit `DocumentUploaded` from file upload handler with `DealId` and filename metadata
- [x] 1.3 Emit `DocumentDeleted` from document delete action in `DocumentUploadService` with `DealId` and filename metadata
- [x] 1.4 Inject `IActivityTracker` into `DocumentUploadService` and emit `DocumentAccessDenied` from `VerifyDealOwnershipAsync` on access denial
- [x] 1.5 Emit `DocumentScanFailed` from `DocumentUploadService` when virus scan fails, with `DealId` and filename
- [x] 1.6 Emit `DocumentRateLimited` from the upload rate limiter `OnRejected` handler in `Program.cs`
- [x] 1.7 Unit tests: verify each of the 6 events is emitted with correct EventType, DealId, and metadata

## Phase 2: Wire Wizard & Fix PdfExported
- [x] 2.1 Emit `WizardStarted` from the quick analysis entry point (`AnalysisStart.razor` and `Dashboard.razor`)
- [x] 2.2 Emit `WizardCompleted` from `DealReport.razor` when report is assembled
- [x] 2.3 Fix `PdfExported` in `ReportViewer.razor` — pass `DealId` in the `TrackEventAsync` call alongside `PropertyName` metadata
- [x] 2.4 bUnit test: PdfExported includes DealId, Dashboard/DealTabs render without errors

## Phase 3: New Event Types
- [ ] 3.1 Add `DocumentDownloaded` and `OAuthLoginCompleted` to `ActivityEventType` enum
- [ ] 3.2 Emit `DocumentDownloaded` from `/api/documents/{id}/download` endpoint with `DealId` and filename metadata
- [ ] 3.3 Emit `OAuthLoginCompleted` from `ExternalAuthEndpoints.cs` OAuth callback with provider name as metadata
- [ ] 3.4 Unit tests: both new events emitted with correct metadata

## Phase 4: Admin Activity Dashboard
- [ ] 4.1 Create `/admin/activity` page (`AdminActivity.razor`) with `[Authorize(Roles = "Admin")]`
- [ ] 4.2 Add recent sessions table — MudDataGrid showing UserId, ConnectedAt, DisconnectedAt, event count, IP address
- [ ] 4.3 Add event log table — MudDataGrid showing EventType, UserId, PageUrl, DealId, IpAddress, Metadata, OccurredAt
- [ ] 4.4 Add filters: user selector, event type dropdown, date range picker, deal ID search
- [ ] 4.5 Add navigation link to admin sidebar/menu for the activity page
- [ ] 4.6 bUnit test: dashboard renders with admin role, rejects non-admin access
