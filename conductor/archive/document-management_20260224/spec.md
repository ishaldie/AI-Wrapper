# Spec: Document Management

## Overview
Close three document-handling gaps and add sender authorization: (1) automatically match uploaded documents to checklist items by filename/type, (2) add download buttons so users can retrieve previously uploaded files, (3) add inbound email ingestion so documents emailed to a deal-specific address are automatically uploaded and matched, and (4) allow account owners to authorize additional email addresses (assistants, brokers, lenders) that can send documents on their behalf.

## Requirements

1. **Auto-match uploads to checklist items** — When a document is uploaded (via chat or checklist tab), scan the filename and `DocumentType` against `ChecklistTemplate.ItemName` keywords. If a match is found, call `DealChecklistItem.MarkSatisfied(documentId)` to link the document and set status to Satisfied. User can reassign a document to a different checklist item via dropdown.

2. **Document download** — Add a download endpoint (`/api/documents/{id}/download`) that streams the file from `IFileStorageService.GetFileAsync` with ownership verification. Show download links:
   - On checklist items that have a linked `DocumentId`
   - In a "Documents" panel listing all uploaded docs for the deal

3. **Checklist file upload** — Add an upload button per checklist item so users can attach documents directly from the Checklist tab (not just from chat). Uploaded doc flows through existing `DocumentUploadService` pipeline then auto-matches.

4. **Authorized Senders** — New `AuthorizedSender` entity linked to the user account. Account owners can add/remove trusted email addresses (assistant, broker, lender, etc.) from a settings page. Each entry stores: email address, label/name, and added date.

5. **Inbound email ingestion** — Expose a webhook endpoint (e.g., SendGrid Inbound Parse) that receives emails sent to a deal-specific address (e.g., `deal-{short-id}@ingest.zsrunderwriting.com`). Security flow:
   - Verify sender email matches either the deal owner's account email OR an authorized sender on that account
   - Reject and discard emails from unrecognized senders
   - Log all rejected attempts for audit
   - Extract attachments from verified emails, run through existing `DocumentUploadService` pipeline (validation, virus scan, storage)
   - Auto-match attachments to checklist items

## New Domain Entities

- **AuthorizedSender** — UserId (FK -> ApplicationUser), Email, Label, CreatedAt
- **EmailIngestionLog** — Id, DealId?, SenderEmail, Status (Accepted/Rejected), Reason, AttachmentCount, CreatedAt

## Deal Entity Changes

- Add `ShortCode` (string, 8-char unique) to Deal for email address generation (`deal-{shortcode}@ingest...`)

## Acceptance Criteria

- [ ] Uploading a doc in chat auto-matches to the best checklist item and marks Satisfied
- [ ] Uploading a doc from the Checklist tab links it and marks Satisfied
- [ ] Matched checklist items show the filename with a download icon
- [ ] Download streams the correct file with ownership verification
- [ ] All uploaded documents for a deal are listed with download links
- [ ] User can reassign a document to a different checklist item
- [ ] Account owners can add/remove authorized sender emails from settings
- [ ] Inbound email webhook receives attachments and processes them into the deal
- [ ] Emails from the deal owner's email are accepted
- [ ] Emails from authorized senders on the owner's account are accepted
- [ ] Emails from unrecognized senders are rejected and logged
- [ ] Existing tests pass; new tests cover matching, download, authorized senders, and email parsing

## Out of Scope

- Email reply/outbound (receive-only)
- OCR or AI-based document classification (match by filename keywords only)
- Bulk upload UI (one file at a time per checklist item)
- Third-party document management integrations (SharePoint, Box, etc.)
- Per-deal sender authorization (account-level only)
