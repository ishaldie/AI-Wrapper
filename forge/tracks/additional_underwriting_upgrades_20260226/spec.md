# Spec: Additional Underwriting Upgrades

**Track ID:** `additional_underwriting_upgrades_20260226`
**Type:** feature

## Overview

Comprehensive expansion of the ZSR Underwriting platform to support all commercial real estate property types encountered in professional practice, incorporating knowledge from 11 base underwriting models (Bridge, CMBS, Multifamily Agency, Hospitality, Proprietary) and 20+ training materials (NREC Bridge Workbook, MBA Section 232 Healthcare program, Freddie Mac Forward, LIHTC Basics, Fannie Mae Submission Guide).

The current system supports 5 property types (Multifamily, AssistedLiving, SkilledNursing, MemoryCare, CCRC) with a single OpEx ratio per type and LTV-only loan sizing. This track upgrades to 12 property types, detailed expense line items with PUPA minimums, dual-constraint loan sizing (LTV + DSCR), and property-type-calibrated AI analysis.

## Requirements

### R1: Expanded Property Types
Add 7 new property types to the PropertyType enum:
1. **Bridge** — Short-term, floating-rate, value-add multifamily (As-Is + Stabilized dual scenario)
2. **Hospitality** — Hotels/motels (ADR × Rooms × Occupancy × 365 revenue model)
3. **Commercial** — Office, retail, industrial (tenant lease-driven NRI, expense stops)
4. **LIHTC** — Tax credit affordable housing (AMI-restricted rents, 4%/9% credits)
5. **BoardAndCare** — Small senior facilities (min 20 beds for HUD, Keys Amendment regulated)
6. **IndependentLiving** — Senior housing with meals/housekeeping but no medical care
7. **SeniorApartment** — Age-restricted (55+/62+) apartments, no services beyond activities

### R2: Protocol Defaults per Property Type (12 types)
Each property type gets calibrated defaults derived from the training materials:
- Target occupancy (e.g., Hospitality 65%, Bridge 90% As-Is / 95% Stabilized)
- Operating expense ratio (fallback when detailed expenses not provided)
- Management fee % (e.g., SNF 5%, Multifamily 3-4%, Hospitality 3%)
- Replacement reserve PUPA (e.g., $250 standard, $200 new build, $350 HUD healthcare)
- DSCR minimum threshold (e.g., 1.25x agency, 1.50x bridge, 1.45x healthcare)
- LTV maximum (e.g., 80% agency, 75% bridge, 85% HUD healthcare)
- Revenue growth rate and expense growth rate defaults
- Other income ratio
- AI system role prompt

### R3: Detailed Expense Line Items
Replace single OpEx ratio with optional detailed expense categories (all property types):
- Real Estate Taxes
- Insurance
- Utilities (electric, gas, water/sewer combined)
- Repairs & Maintenance (min $600 PUPA)
- Payroll & Benefits (min $1,000 PUPA for staffed properties)
- Marketing/Advertising (min $50 PUPA)
- General & Administrative (min $250 PUPA)
- Management Fee (% of EGI, type-specific default)
- Replacement Reserves (PUPA, type-specific default)
- Other Expenses (ground rent, professional fees, etc.)

When detailed expenses are provided, they override the ratio-based calculation. When not provided, the system falls back to the OpEx ratio.

### R4: Dual-Constraint Loan Sizing (LTV + DSCR)
Maximum loan = MIN(LTV-based amount, DSCR-based amount):
- **LTV-based:** PurchasePrice × (MaxLTV / 100) — already implemented
- **DSCR-based:** NOI / (MinDSCR × MortgageConstant) — new
  - MortgageConstant = annual debt service per $1 of loan (function of rate + amortization)
  - The constraining test is reported in the output

### R5: Property-Type-Specific AI Prompts
Each property type gets a specialized Claude system role and financial context format:
- **Bridge:** Focus on value-add thesis, stabilization timeline, exit strategy, As-Is vs Stabilized comparison
- **Hospitality:** RevPAR analysis, seasonal patterns, flag/franchise value, STR comp set
- **Commercial:** Tenant credit analysis, lease rollover risk, TI/LC reserves, market rent vs in-place
- **LIHTC:** Compliance risk, LURA expiration, restricted rent adequacy, investor equity
- **BoardAndCare:** State licensing, Keys Amendment compliance, small-facility operational risk
- **IndependentLiving:** Service package adequacy, age demographics, meal program economics
- **SeniorApartment:** Age restriction compliance, MAP vs LEAN eligibility, demand drivers
- Existing types (Multifamily, SNF, ALF, MC, CCRC) retain their current prompt logic

### R6: Updated Validators, Parsers, and Import
- DealInputValidator: New property-type-specific conditional rules
- BulkImportRowValidator: Accept new property types
- PortfolioImportParser: Parse new type names from import files
- PortfolioTemplateGenerator: Include new types in template

### R7: Updated Report Assembly
- ReportAssembler operations section shows detailed expense line items when available
- Financial analysis shows constraining loan sizing test (LTV or DSCR)
- Sources & Uses includes both loan constraints with labels

### R8: UI Updates
- DealTabs property type dropdown includes all 12 types
- Deal entry form shows property-type-specific fields
- Deal Pipeline grid recognizes new types

## Technical Approach

### Architecture
- Domain: Expand PropertyType enum, add DetailedExpenses value object
- Application: Expand ProtocolDefaults with full type matrix, update DTOs, validators
- Infrastructure: Update calculations, report assembly, parsers, migration
- Web: Update UI components for new types

### Key Decisions
- Detailed expenses stored as JSON blob on Deal entity (consistent with CmsData pattern) rather than separate table — avoids migration complexity
- DSCR loan sizing implemented in existing UnderwritingCalculator alongside LTV
- Property type defaults encoded in ProtocolDefaults.cs as static dictionaries — single source of truth
- Backward compatible: existing deals with old property types continue to work

## Acceptance Criteria

- [ ] All 12 property types selectable in Deal Entry UI
- [ ] ProtocolDefaults returns correct occupancy, OpEx ratio, reserves, DSCR/LTV caps per type
- [ ] Detailed expenses can be entered and override ratio-based calculation
- [ ] Loan sizing shows MIN(LTV-based, DSCR-based) with constraining test labeled
- [ ] AI prompts use property-type-specific system roles for all 12 types
- [ ] Bulk import accepts all 12 property type names
- [ ] Report shows detailed expense breakdown when available
- [ ] All existing tests continue to pass
- [ ] New tests cover each new property type's defaults and calculations

## Out of Scope

- Debt Yield and LTC loan sizing constraints (future track)
- As-Is vs Stabilized dual-scenario engine for Bridge (future track — currently single scenario)
- Full rent roll unit-level detail input
- Comp analysis engine (expense/rent/sales comps)
- LIHTC tax credit calculation engine (qualified basis, credit amounts)
- Hospitality STR/RevPAR comp integration
- Commercial tenant lease schedule input
