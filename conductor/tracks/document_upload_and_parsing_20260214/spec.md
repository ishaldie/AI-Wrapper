# Spec: Document Upload & Parsing

## Overview
Build file upload functionality for deal-related documents (rent rolls, T12/P&L, offering memorandums, appraisals, Phase I/PCA, loan term sheets). Parse uploaded documents to extract data that overrides default assumptions per the underwriting protocol.

## Requirements
1. File upload component supporting PDF, XLSX, CSV, DOCX formats
2. Document type classification (Rent Roll, T12/P&L, OM, Appraisal, Phase I/PCA, Loan Term Sheet)
3. Store uploaded files in local file storage (or Azure Blob later)
4. Parse rent rolls to extract: unit mix, in-place rents, occupancy, lease expirations
5. Parse T12/P&L to extract: actual revenue, expenses, NOI line items
6. Parse loan term sheets to extract: rate, LTV, IO period, amortization, prepayment
7. Parsed data overrides RealAI estimates and default assumptions
8. Display parsed data summary for user verification before applying
9. Support multiple documents per deal
10. Maximum file size: 25MB per file

## Acceptance Criteria
- [ ] Users can upload files via drag-and-drop or file picker
- [ ] Uploaded files are stored and associated with the correct deal
- [ ] Document type is selected by user or auto-detected
- [ ] Rent roll parsing extracts unit count, rents, occupancy
- [ ] T12 parsing extracts revenue and expense line items
- [ ] Parsed data shown to user for confirmation before applying
- [ ] Overrides are flagged in the underwriting report (Source: User-Provided)
- [ ] File size validation prevents uploads over 25MB

## Out of Scope
- OCR for scanned PDFs (future enhancement)
- AI-powered document extraction (can add later with Claude)
- Cloud storage (start with local, migrate to Azure Blob later)
