# Plan: Excel Model Export

## Phase 1: Core Service & All Sheets
- [x] Create `IExcelModelExporter` interface in Application/Interfaces
- [x] Create `ExcelModelExporter` class in Infrastructure/Services with OpenXml workbook scaffolding
- [x] Implement property-type routing (Multifamily vs Commercial templates)
- [x] Multifamily: Year 1 Pro-forma (monthly columns), Underwriting (10-year), Rent Roll, Sources & Uses, Comps
- [x] Commercial: Loan Sizing Summary, Underwriting (CREFC P&L), Cash Flow, Sources & Uses, Comps
- [x] Register service in DI container
- [x] Professional formatting: stylesheet, column widths, freeze panes, dark headers

## Phase 2: UI Integration & Download
- [x] Add "Export Model" button to DealTabs.razor header (next to Generate Report)
- [x] Update download.js to support xlsx MIME type
- [x] Implement JS interop blob download via DotNetStreamReference
- [x] Add loading state and error handling

## Phase 3: Testing & Verification
- [ ] Write unit test for multifamily export
- [ ] Write unit test for commercial export
- [ ] End-to-end test: export from running app
- [ ] Handle edge cases: deals with no rent roll, no comps, no monthly actuals
