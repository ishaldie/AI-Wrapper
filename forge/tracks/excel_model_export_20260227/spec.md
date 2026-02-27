# Spec: Excel Model Export

## Overview
Export a deal's underwriting data into a multi-sheet Excel workbook with property-type-specific templates. Multifamily deals use a monthly pro-forma layout with unit-level rent roll analysis. Commercial deals use a CMBS-style layout with CREFC P&L and loan sizing. The export produces a professional-grade underwriting model for further modeling, lender submissions, and committee presentations.

## Requirements

### Multifamily Template (PropertyType = Multifamily, LIHTC, Bridge)
1. **Year 1 Pro-forma** (first tab — monthly columns)
   - 12 monthly columns + Annual Total + Per Unit
   - Revenue: GPR, Loss to Lease, Concessions, Vacancy Loss, Bad Debt, Other Income
   - Expenses: Payroll, R&M, Make Ready, Marketing, G&A, Utilities, Mgmt Fees, Taxes, Insurance, Reserves
   - NOI

2. **Underwriting** (10-year projection)
   - Property details, capitalization summary
   - Historical T-12 operating statement
   - Investment returns (IRR, Cash-on-Cash, Equity Multiple)
   - DSCR / Debt Yield per year

3. **Rent Roll** — unit-level detail with beds/baths/SF/rent/status/tenant

4. **Sources & Uses** — deal-level capital stack and use of proceeds

5. **Comps** — securitization comparables with market summary

### Commercial Template (PropertyType = Commercial, Hospitality, etc.)
1. **Loan Sizing Summary** (first tab — dashboard view)
   - Deal info, pricing, loan sizing, debt metrics, return metrics, NOI summary, capital stack

2. **Underwriting** (CREFC-format P&L)
   - Revenue and expense line items with Annual $, $/Unit, % of EGI columns

3. **Cash Flow** — historical actuals + projected cash flows

4. **Sources & Uses** — same as multifamily

5. **Comps** — same as multifamily

### Shared
- **Export Action**: Button in deal header (next to existing PDF export)
- Downloads .xlsx via JS interop blob
- Filename: `{PropertyName}_UW_Model_{date}.xlsx`

## Technical Approach
- `IExcelModelExporter` interface in Application/Interfaces
- `ExcelModelExporter` in Infrastructure/Services using DocumentFormat.OpenXml
- Checks `deal.PropertyType` to select multifamily vs commercial template
- JS interop download via existing blob pattern from ReportPdfExporter
- Each sheet built by a dedicated private method

## Acceptance Criteria
- [ ] Export button visible on deal detail page
- [ ] Clicking export downloads a valid .xlsx file
- [ ] Multifamily deals get monthly pro-forma + rent roll sheets
- [ ] Commercial deals get loan sizing + CREFC P&L sheets
- [ ] Empty/null fields handled gracefully (show blank, not errors)
- [ ] File opens correctly in Excel and Google Sheets
- [ ] Professional formatting: headers, number formats, column widths

## Out of Scope
- Editable formulas / live calculation in Excel (data snapshot export)
- Import from Excel back into the app
- Template customization UI
- Charts/graphs in the workbook
- Waterfall / partnership distributions (future track)
