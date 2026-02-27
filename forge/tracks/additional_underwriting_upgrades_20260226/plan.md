# Implementation Plan: Additional Underwriting Upgrades

## Phase 1: Domain & Enum Expansion

- [ ] Task: Add 7 new values to PropertyType enum (Bridge, Hospitality, Commercial, LIHTC, BoardAndCare, IndependentLiving, SeniorApartment) in `src/ZSR.Underwriting.Domain/Enums/PropertyType.cs`
- [ ] Task: Add DetailedExpenses record/class with all 10 expense line-item fields (RealEstateTaxes, Insurance, Utilities, RepairsAndMaintenance, Payroll, Marketing, GeneralAndAdmin, ManagementFee, ReplacementReserves, OtherExpenses) in `src/ZSR.Underwriting.Domain/Entities/` or as value object
- [ ] Task: Add `DetailedExpensesJson` nullable string property to Deal entity for JSON storage (consistent with CmsData pattern)
- [ ] Task: Write tests — PropertyType enum has 12 values, DetailedExpenses serialization round-trips correctly
- [ ] Task: Phase 1 Manual Verification

---

## Phase 2: ProtocolDefaults Expansion

- [ ] Task: Add occupancy defaults for all 7 new types — Bridge 92%, Hospitality 65%, Commercial 93%, LIHTC 97%, BoardAndCare 85%, IndependentLiving 90%, SeniorApartment 95%
- [ ] Task: Add OpEx ratio defaults for all 7 new types — Bridge 50%, Hospitality 62%, Commercial 45%, LIHTC 58%, BoardAndCare 70%, IndependentLiving 60%, SeniorApartment 52%
- [ ] Task: Add other income ratio defaults — Bridge 10%, Hospitality 15% (F&B, parking), Commercial 3%, LIHTC 5%, BoardAndCare 5%, IndependentLiving 8%, SeniorApartment 10%
- [ ] Task: Add management fee defaults per type — Multifamily 3.5%, Bridge 3.5%, Hospitality 3%, Commercial 4%, LIHTC 6%, SNF 5%, ALF 5%, MemoryCare 5%, CCRC 5%, BoardAndCare 5%, IndependentLiving 5%, SeniorApartment 4%
- [ ] Task: Add replacement reserve PUPA defaults — $250 standard, $200 new build equivalent, $350 HUD healthcare (SNF, ALF, MC, CCRC, B&C)
- [ ] Task: Add DSCR minimum thresholds per type — Multifamily 1.25, Bridge 1.20, Hospitality 1.40, Commercial 1.30, LIHTC 1.15, SNF 1.45, ALF 1.45, MemoryCare 1.45, CCRC 1.40, BoardAndCare 1.45, IndependentLiving 1.35, SeniorApartment 1.25
- [ ] Task: Add LTV maximum per type — Multifamily 80%, Bridge 75%, Hospitality 65%, Commercial 75%, LIHTC 85%, SNF 85%, ALF 85%, MemoryCare 80%, CCRC 80%, BoardAndCare 80%, IndependentLiving 80%, SeniorApartment 80%
- [ ] Task: Add revenue/expense growth rate defaults — revenue 3% standard, 2% LIHTC; expenses 3% fixed / 2% controllable
- [ ] Task: Add helper methods — `IsSeniorHousing()` updated to include BoardAndCare, IndependentLiving, SeniorApartment; `IsHealthcare()` for SNF/ALF/MC/CCRC/B&C; `GetMinDscr()`, `GetMaxLtv()`, `GetManagementFeePct()`, `GetReservesPupa()`
- [ ] Task: Add detailed expense PUPA minimums dictionary — R&M $600, Payroll $1000 (staffed properties), Marketing $50, G&A $250
- [ ] Task: Write tests — every new property type returns correct defaults for occupancy, OpEx, DSCR, LTV, reserves, management fee, other income
- [ ] Task: Phase 2 Manual Verification

---

## Phase 3: DTOs, Validators & Parsers

- [ ] Task: Add `DetailedExpenses` property to DealInputDto — nullable object with all 10 fields, populated only when user provides line-item expenses
- [ ] Task: Add `ManagementFeePct` field to DealInputDto for explicit management fee override
- [ ] Task: Update DealInputValidator — add property-type-specific conditional rules for new types (e.g., Hospitality requires rooms/ADR; Commercial allows null UnitCount; LIHTC requires UnitCount)
- [ ] Task: Update BulkImportRowValidator — accept all 12 PropertyType values
- [ ] Task: Update PortfolioImportParser header aliases — add hospitality aliases (rooms, adr, revpar), commercial aliases (nra, sqft), LIHTC aliases
- [ ] Task: Update PortfolioTemplateGenerator — include all 12 types in property type dropdown/instructions
- [ ] Task: Update BulkImportRowDto if needed for new type-specific fields
- [ ] Task: Write tests — validators accept/reject correctly for each new type; parser recognizes new header aliases; template includes new types
- [ ] Task: Phase 3 Manual Verification

---

## Phase 4: Loan Sizing — DSCR Constraint

- [ ] Task: Add `CalculateMortgageConstant(annualRate, amortizationYears, isInterestOnly)` method to UnderwritingCalculator — returns annual debt service per $1 of loan
- [ ] Task: Add `CalculateMaxLoanByDscr(noi, minDscr, mortgageConstant)` method — returns NOI / (minDscr × mortgageConstant)
- [ ] Task: Add `CalculateConstrainedLoan(purchasePrice, maxLtv, noi, minDscr, rate, amortization, isInterestOnly)` method — returns MIN(LTV-based, DSCR-based) plus which test constrains
- [ ] Task: Update ReportAssembler financial analysis section — show both loan amounts and label the constraining test
- [ ] Task: Write tests — DSCR-constrained loan < LTV-constrained when NOI is low; LTV-constrained when NOI is high; interest-only mortgage constant; 30-year amortized constant
- [ ] Task: Phase 4 Manual Verification

---

## Phase 5: Detailed Expense Calculations

- [ ] Task: Add `CalculateDetailedExpenses(detailedExpenses, unitCount, egiForMgmtFee, propertyType)` method to UnderwritingCalculator — sums all line items, applies PUPA minimums where applicable, calculates management fee from EGI × %
- [ ] Task: Update `CalculateOperatingExpenses` to prefer detailed expenses over ratio when available
- [ ] Task: Update ReportAssembler operations section — when detailed expenses exist, show each line item (total, per-unit, % of EGI) instead of single OpEx line
- [ ] Task: Write tests — detailed expenses sum correctly; PUPA minimums enforced (R&M floors to $600 × units); management fee calculates from EGI; fallback to ratio when no detailed expenses
- [ ] Task: Phase 5 Manual Verification

---

## Phase 6: AI Prompt Expansion

- [ ] Task: Add system role prompts for Bridge ("bridge loan underwriting analyst specializing in value-add multifamily — focus on stabilization timeline, renovation scope, exit strategy, As-Is vs Stabilized comparison")
- [ ] Task: Add system role prompts for Hospitality ("hospitality real estate underwriting analyst — ADR/RevPAR analysis, seasonal occupancy patterns, flag/franchise value, PIP requirements, F&B operations")
- [ ] Task: Add system role prompts for Commercial ("commercial real estate underwriting analyst — tenant credit analysis, lease rollover risk, TI/LC reserves, market rent analysis, expense stops/reimbursements")
- [ ] Task: Add system role prompts for LIHTC ("affordable housing underwriting analyst — LIHTC compliance, AMI-restricted rents, LURA/regulatory agreement terms, tax credit equity structure, below-market debt advantages")
- [ ] Task: Add system role prompts for BoardAndCare, IndependentLiving, SeniorApartment (healthcare/senior specialist variants with appropriate focus)
- [ ] Task: Update `AppendPropertyHeader` to show type-appropriate labels (rooms for hospitality, NRA for commercial, beds for healthcare, units for residential)
- [ ] Task: Update `AppendFinancialMetrics` to include DSCR constraint info and detailed expenses when available
- [ ] Task: Write tests — each property type produces a prompt containing the expected system role keywords
- [ ] Task: Phase 6 Manual Verification

---

## Phase 7: Service Layer & Migration

- [ ] Task: Update DealService.CreateDealAsync/UpdateDealAsync — map DetailedExpenses JSON to/from Deal entity; handle new property types
- [ ] Task: Update DealService.GetDealAsync — deserialize DetailedExpenses from JSON back to DTO
- [ ] Task: Add EF Core migration for DetailedExpensesJson column on Deal table and any new fields
- [ ] Task: Update BulkImportService — map new property types and optional detailed expense fields from import rows
- [ ] Task: Write tests — Deal round-trips with new property types and detailed expenses; bulk import handles new types
- [ ] Task: Phase 7 Manual Verification

---

## Phase 8: UI Updates

- [ ] Task: Update DealTabs.razor PropertyType dropdown — show all 12 types with grouped categories (Residential: Multifamily, Bridge, LIHTC, SeniorApartment; Healthcare: ALF, SNF, MemoryCare, CCRC, BoardAndCare, IndependentLiving; Commercial: Commercial, Hospitality)
- [ ] Task: Add optional "Detailed Expenses" expansion panel to deal entry — toggleable section with all 10 expense line items, pre-populated with PUPA minimums as placeholders
- [ ] Task: Update DealPipeline.razor — recognize and display all 12 property types with appropriate icons/labels
- [ ] Task: Update PortfolioImport.razor — show new property types in help text / template download
- [ ] Task: Phase 8 Manual Verification

---

## Phase 9: Integration Testing & Polish

- [ ] Task: End-to-end test — create deal of each new property type, verify defaults populate, run report assembly, confirm AI prompts contain correct role
- [ ] Task: Verify backward compatibility — existing Multifamily and senior housing deals unchanged
- [ ] Task: Verify bulk import with CSV containing mix of all 12 property types
- [ ] Task: Fix any compilation errors or runtime issues discovered during integration
- [ ] Task: Phase 9 Manual Verification

---
