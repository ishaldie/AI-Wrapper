# Plan: Report Assembly & Display

## Phase 1: Report Assembly Service
- [x] Create `IReportAssembler` interface in Application layer bf2922d
- [x] Implement `ReportAssembler` that combines calculations, RealAI data, and AI prose bf2922d
- [x] Create `ReportSection` DTOs for each of the 10 sections fbe66da
- [x] Create formatting helpers (currency, percentage, multiples per protocol) 928539b
- [x] Write unit tests for assembly logic and formatting bf2922d

## Phase 2: Blazor Report Viewer
- [x] Create `ReportViewer.razor` main page component f7c28b3
- [x] Create `SectionNav.razor` sidebar for section navigation f7c28b3 (inline in ReportViewer)
- [x] Create `CoreMetricsTable.razor` (Section 1) f7c28b3 (inline in ReportViewer)
- [x] Create `ExecutiveSummary.razor` (Section 2) with decision badge f7c28b3 (inline in ReportViewer)
- [x] Create `AssumptionsTable.razor` (Section 3) f7c28b3 (inline in ReportViewer)
- [x] Create `PropertyComps.razor` (Section 4) with adjustment table f7c28b3 (inline in ReportViewer)
- [x] Create `TenantMarketIntel.razor` (Section 5) with benchmarking table f7c28b3 (inline in ReportViewer)
- [x] Create `OperationsP&L.razor` (Section 6) f7c28b3 (inline in ReportViewer)
- [x] Create `FinancialAnalysis.razor` (Section 7) with sub-tables f7c28b3 (inline in ReportViewer)
- [x] Create `ValueCreation.razor` (Section 8) f7c28b3 (inline in ReportViewer)
- [x] Create `RiskAssessment.razor` (Section 9) with color-coded matrix f7c28b3 (inline in ReportViewer)
- [x] Create `InvestmentDecision.razor` (Section 10) f7c28b3 (inline in ReportViewer)

## Phase 3: Styling & PDF Export
- [x] Style decision badge (GO=green, CONDITIONAL=yellow, NO GO=red) f7c28b3 (GetDecisionColor)
- [x] Style risk severity colors in matrix f7c28b3 (GetSeverityColor)
- [~] Implement PDF export using QuestPDF
- [~] Add "Export PDF" button to report viewer
- [~] Write bUnit tests for key report components
- [~] Write integration test for full report assembly + render
