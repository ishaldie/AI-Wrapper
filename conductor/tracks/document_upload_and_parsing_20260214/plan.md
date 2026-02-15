# Plan: Document Upload & Parsing

## Phase 1: File Upload Infrastructure
- [x] Create `IFileStorageService` interface in Domain `0a7070d`
- [x] Implement `LocalFileStorageService` in Infrastructure `0a7070d`
- [x] Create `FileUpload.razor` component with drag-and-drop `8329ddb`
- [x] Add document type selector dropdown `8329ddb`
- [x] Implement file size and format validation `8329ddb`
- [x] Associate uploaded files with Deal entity `cd59882`
- [x] Write tests for file validation and storage `8329ddb`

## Phase 2: Document Parsing
- [ ] Create `IDocumentParser` interface with type-specific implementations
- [ ] Implement `RentRollParser` (XLSX/CSV) — extract units, rents, occupancy, leases
- [ ] Implement `T12Parser` (XLSX/PDF) — extract revenue, expenses, NOI
- [ ] Implement `LoanTermSheetParser` — extract rate, LTV, IO, amortization
- [ ] Create `ParsedDocumentResult` DTO with extracted fields
- [ ] Write unit tests for each parser with sample files

## Phase 3: Override Integration
- [ ] Create `ParsedDataReview.razor` component showing extracted values
- [ ] Add confirm/reject workflow for parsed data
- [ ] Apply confirmed overrides to UnderwritingInput entity
- [ ] Flag overridden fields with source attribution
- [ ] Write integration tests for override application
