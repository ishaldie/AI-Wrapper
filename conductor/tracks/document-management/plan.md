# Plan: Document Management

## Phase 1: Auto-Match Engine
*Status: Complete*

- [x] Task 1.1: Create `DocumentMatchingService` in Application/Services — keyword-based matching of filename + DocumentType against ChecklistTemplate.ItemName
- [x] Task 1.2: Wire auto-match into `DealChatTab` upload flow — after `UploadDocumentAsync`, call matching service, then `MarkSatisfied(documentId)` on best match
- [x] Task 1.3: Wire auto-match into `ApplyDealUpdate` checklist handling — when AI references a document, link if available
- [x] Task 1.4: Unit tests for DocumentMatchingService (exact match, partial match, no match, ambiguous) — completed in Task 1.1 (14 tests)

## Phase 2: Document Download
*Status: Complete*

- [x] Task 2.1: Create download API endpoint `/api/documents/{id}/download` — stream file from IFileStorageService with deal ownership verification
- [x] Task 2.2: Add download icon/link on checklist items with linked DocumentId (MudIconButton with download icon)
- [x] Task 2.3: Add "Documents" panel to DealTabs — list all UploadedDocuments for the deal with filename, type, upload date, size, and download link
- [x] Task 2.4: Integration tests for download endpoint (auth, ownership, file not found, happy path)

## Phase 3: Checklist File Upload
*Status: Complete*

- [x] Task 3.1: Add upload button per checklist item (InputFile + MudIcon per row)
- [x] Task 3.2: Wire upload to existing DocumentUploadService pipeline + MarkSatisfied on the specific checklist item
- [x] Task 3.3: Show upload progress (MudProgressCircular) and success/error feedback (Snackbar)
- [x] Task 3.4: bUnit tests for checklist upload UI interaction (4 tests)

## Phase 4: Authorized Senders
*Status: Complete*

- [x] Task 4.1: Create `AuthorizedSender` entity in Domain/Entities — UserId, Email, Label, CreatedAt
- [x] Task 4.2: Add DbSet + EF configuration in AppDbContext (unique index on UserId + Email)
- [x] Task 4.3: Create EF migration for AuthorizedSender
- [x] Task 4.4: Create `AuthorizedSenderService` in Infrastructure/Services — Add, Remove, List, IsAuthorized (13 tests)
- [x] Task 4.5: Create Authorized Senders settings page (`/settings/authorized-senders`) — MudTable with add/remove, email + label fields
- [x] Task 4.6: Unit tests for AuthorizedSenderService (13 tests)
- [x] Task 4.7: bUnit tests for settings page (5 tests)

## Phase 5: Inbound Email Ingestion
*Status: Complete*

- [x] Task 5.1: Add `ShortCode` property to Deal entity (8-char unique, generated on creation) + migration
- [x] Task 5.2: Display deal email address in DealTabs UI (`deal-{shortcode}@ingest.zsrunderwriting.com`) with copy button
- [x] Task 5.3: Create `EmailIngestionLog` entity — DealId?, SenderEmail, Status (Accepted/Rejected), Reason, AttachmentCount, CreatedAt
- [x] Task 5.4: Add DbSet + EF configuration + migration for EmailIngestionLog
- [x] Task 5.5: Create `EmailIngestionService` in Infrastructure/Services — parse inbound email payload, verify sender against deal owner email + authorized senders, reject unknowns with audit log
- [x] Task 5.6: Create webhook endpoint `/api/ingest/email` — receive SendGrid Inbound Parse payload, delegate to EmailIngestionService
- [x] Task 5.7: Process verified attachments — run through DocumentUploadService pipeline, then auto-match to checklist items via DocumentMatchingService
- [x] Task 5.8: Unit tests for EmailIngestionService (9 tests — owner email accepted, authorized sender accepted, unknown rejected + log, accepted log, multiple attachments, no deal found, case-insensitive, attachment count log)
- [x] Task 5.9: Integration tests for webhook endpoint (4 tests — multi-email logging, revoked sender, malformed To address, empty attachments)

## Phase 6: Chat Side Panel
*Status: Pending*

- [ ] Task 6.1: Refactor DealTabs layout — remove Chat from MudTabs (5 tabs → 4), add collapsible side panel for DealChatTab on the right side of the page
- [ ] Task 6.2: Add toggle button (MudIconButton or FAB) to open/close the chat panel — persist open/close state during session
- [ ] Task 6.3: Responsive layout — chat panel takes ~40% width when open, tabs expand to full width when closed; stack vertically on mobile
- [ ] Task 6.4: Ensure OnDealUpdated callback still refreshes tab data when chat is in side panel
- [ ] Task 6.5: Update `/deals/{id}?tab=chat` route handling — open chat panel instead of switching to a tab
- [ ] Task 6.6: bUnit tests for side panel rendering, toggle behavior, and tab count

## Phase 7: Document Reassignment & Polish
*Status: Pending*

- [ ] Task 7.1: Add reassignment dropdown on checklist items — user can change which checklist item a document is linked to
- [ ] Task 7.2: Show ingestion log in deal (recent email activity — accepted/rejected with sender and timestamp)
- [ ] Task 7.3: CSS polish — consistent styling for upload buttons, download links, document panels, chat side panel
- [ ] Task 7.4: Verify all existing tests pass + coverage for new features
