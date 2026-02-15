# Plan: Report Assembly & Display

## Phase 1: Report Assembly Service
- [x] Create `IReportAssembler` interface in Application layer bf2922d
- [x] Implement `ReportAssembler` that combines calculations, RealAI data, and AI prose bf2922d
- [x] Create `ReportSection` DTOs for each of the 10 sections fbe66da
- [x] Create formatting helpers (currency, percentage, multiples per protocol) 928539b
- [x] Write unit tests for assembly logic and formatting bf2922d

## Phase 2: Blazor Report Viewer
- [ ] Create `ReportViewer.razor` main page component
- [ ] Create `SectionNav.razor` sidebar for section navigation
- [ ] Create `CoreMetricsTable.razor` (Section 1)
- [ ] Create `ExecutiveSummary.razor` (Section 2) with decision badge
- [ ] Create `AssumptionsTable.razor` (Section 3)
- [ ] Create `PropertyComps.razor` (Section 4) with adjustment table
- [ ] Create `TenantMarketIntel.razor` (Section 5) with benchmarking table
- [ ] Create `OperationsP&L.razor` (Section 6)
- [ ] Create `FinancialAnalysis.razor` (Section 7) with sub-tables
- [ ] Create `ValueCreation.razor` (Section 8)
- [ ] Create `RiskAssessment.razor` (Section 9) with color-coded matrix
- [ ] Create `InvestmentDecision.razor` (Section 10)

## Phase 3: Styling & PDF Export
- [ ] Style decision badge (GO=green, CONDITIONAL=yellow, NO GO=red)
- [ ] Style risk severity colors in matrix
- [ ] Implement PDF export using QuestPDF or IronPDF
- [ ] Add "Export PDF" button to report viewer
- [ ] Write bUnit tests for key report components
- [ ] Write integration test for full report assembly + render
