# Plan: Document Upload & Parsing

## Phase 1: File Upload Infrastructure
- [x] Create `IFileStorageService` interface in Domain `0a7070d`
- [x] Implement `LocalFileStorageService` in Infrastructure `0a7070d`
- [x] Create `FileUpload.razor` component with drag-and-drop `8329ddb`
- [x] Add document type selector dropdown `8329ddb`
- [x] Implement file size and format validation `8329ddb`
- [x] Associate uploaded files with Deal entity `cd59882`
- [x] Write tests for file validation and storage `8329ddb`
[checkpoint: phase1-verified — 47 tests passing, user approved 2026-02-15]

## Phase 2: Document Parsing
- [x] Create `IDocumentParser` interface with type-specific implementations `6d20972`
- [x] Implement `RentRollParser` (XLSX/CSV) — extract units, rents, occupancy, leases `6d20972`
- [x] Implement `T12Parser` (XLSX/CSV) — extract revenue, expenses, NOI `6d20972`
- [x] Implement `LoanTermSheetParser` — extract rate, LTV, IO, amortization `6d20972`
- [x] Create `ParsedDocumentResult` DTO with extracted fields `6d20972`
- [x] Write unit tests for each parser with sample files `6d20972`

## Phase 3: Override Integration
- [ ] Create `ParsedDataReview.razor` component showing extracted values
- [ ] Add confirm/reject workflow for parsed data
- [ ] Apply confirmed overrides to UnderwritingInput entity
- [ ] Flag overridden fields with source attribution
- [ ] Write integration tests for override application
