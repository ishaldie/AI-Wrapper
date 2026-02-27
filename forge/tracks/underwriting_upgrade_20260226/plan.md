# Implementation Plan: Underwriting Upgrade — Fannie Mae Compliance

## Phase 1: Domain Model — Fannie Mae Product Types

- [x] Task: Create `FannieProductType` enum with 14 values (Conventional, SmallLoan, AffordableHousing, SeniorsIL, SeniorsAL, SeniorsALZ, StudentHousing, ManufacturedHousing, Cooperative, SARM, GreenRewards, Supplemental, NearStabilization, ROAR)
- [x] Task: Create `FannieProductProfile` value object with LTV max, DSCR min, amortization max, min loan, min occupancy, and eligibility flags
- [x] Task: Create `FannieProductProfiles` static registry that maps each `FannieProductType` to its official term sheet parameters
- [x] Task: Add `FannieProductType` property to `Deal` entity (nullable, only set when ExecutionType = FannieMae)
- [x] Task: Create EF Core migration for `FannieProductType` column on Deals table
- [x] Task: Write unit tests — verify all 14 product profiles have correct LTV, DSCR, and amortization values matching official Fannie Mae term sheets
- [x] Task: Phase 1 Manual Verification

## Phase 2: Calculation Engine — Product-Aware Underwriting

- [x] Task: Refactor `CalculationResultAssembler` to accept `FannieProductType` and apply product-specific DSCR min, LTV max, and amortization cap when sizing debt
- [x] Task: Add `CalculateSeniorsBlendedDscr()` to `UnderwritingCalculator` — blends IL (1.30x), AL (1.40x), ALZ (1.45x) based on bed count ratios from Deal entity
- [x] Task: Add `CalculateCooperativeDualDscr()` — returns pass/fail for both actual operations (1.00x) and market rental basis (1.55x) tests
- [x] Task: Add `CalculateSarmStressDscr()` — calculates DSCR at maximum note rate (Margin + Cap Strike) instead of actual rate
- [x] Task: Add `CalculateGreenRewardsNcfAdjustment()` — adds 75% of owner-projected + 25% of tenant-projected energy/water savings to Underwritten NCF
- [x] Task: Add MHC 5% minimum vacancy floor enforcement in `CalculateVacancyLoss()` when product type is ManufacturedHousing
- [x] Task: Add `CalculateSnfNcfCapTest()` — flags if Skilled Nursing NCF exceeds 20% of total property NCF
- [x] Task: Add `CalculateRoarRehabDscr()` — calculates rehab-period DSCR (1.0x IO / 0.75x amortizing) and stabilized DSCR separately
- [x] Task: Add `CalculateSupplementalCombinedTest()` — tests combined DSCR and combined LTV across senior + supplemental loans
- [x] Task: Store product compliance results (pass/fail for each test) on `CalculationResult` entity as JSON
- [x] Task: Write unit tests for each new calculation method with edge cases (zero beds, boundary DSCR values, mixed bed types)
- [x] Task: Phase 2 Manual Verification

## Phase 3: Risk Rating — Product-Aware Thresholds

- [x] Task: Refactor `RiskRatingCalculator.RateDscr()` to accept `FannieProductProfile` and use product-specific min DSCR as baseline instead of hardcoded 1.25x
- [x] Task: Add `RateSeniorsSkilledNursing()` — SNF NCF > 20% → Critical, > 15% → High, > 10% → Moderate
- [x] Task: Add `RateStudentEnrollment()` — enrollment < 10K for Dedicated → High, < 5K → Critical
- [x] Task: Add `RateMhcTenantOccupied()` — tenant-occupied homes > 35% → High, > 50% → Critical
- [x] Task: Add `RateCoopSponsorConcentration()` — single sponsor > 40% → Moderate, > 60% → High
- [x] Task: Add `RateAffordableSubDebt()` — hard sub combined DSCR < 1.05x → Critical
- [x] Task: Create `FannieComplianceRiskAssessment` that runs all applicable product tests and returns a compliance summary
- [x] Task: Write unit tests for all new risk rating methods
- [x] Task: Phase 3 Manual Verification

## Phase 4: Report Prose — Fannie Mae Compliance Context

- [x] Task: Update `UnderwritingPromptBuilder` to include `FannieProductType` and its key parameters (DSCR, LTV, amortization) in all prompt templates
- [x] Task: Update `BuildRiskAssessmentPrompt()` to include product-specific compliance test results (SNF cap, dual DSCR, stress test) so Claude can narrate compliance status
- [x] Task: Update `BuildInvestmentDecisionPrompt()` to use product-aware GO/NO GO logic — replace hardcoded IRR > 15% / DSCR > 1.5x with product min DSCR and Fannie compliance pass/fail
- [x] Task: Update `BuildExecutiveSummaryPrompt()` to identify the Fannie Mae product type, execution path, and key compliance metrics in the header
- [x] Task: Add `BuildFannieComplianceSummary()` helper that formats compliance test results as structured text for prompt injection
- [x] Task: Write integration tests verifying prompts include product-specific data for Seniors, Student, SARM, and Cooperative deal types
- [x] Task: Phase 4 Manual Verification [checkpoint: f650a07]

## Phase 5: Deal Entry UI — Product Type Selection

- [x] Task: Add `FannieProductType` dropdown to DealTabs.razor, visible only when ExecutionType = FannieMae
- [x] Task: Implement auto-suggestion logic: when user selects PropertyType, pre-populate FannieProductType (Multifamily→Conventional, AssistedLiving→SeniorsAL, etc.) with ability to override
- [x] Task: Add conditional fields per product type: Student Housing → enrollment count, university distance; MHC → pad site count, tenant-occupied %; Green → projected energy savings, improvement budget; Cooperative → operating reserve balance, sponsor ownership %; SARM → cap strike rate, index
- [x] Task: Show product-specific compliance summary card on the Analysis tab — displays LTV cap, DSCR min, amortization max, and pass/fail status for each applicable test
- [x] Task: Phase 5 Manual Verification [checkpoint: ebcb9f8]

## Phase 6: Checklist Enhancement

- [x] Task: Add `FannieProductType` filter column to `ChecklistTemplate` entity alongside existing ExecutionType and TransactionType
- [x] Task: Update `ChecklistTemplateSeed` to tag existing items with applicable FannieProductType values and add new product-specific required documents (Seniors: Mgmt/Ops/Regulatory reports; Student: enrollment verification; MHC: flood zone analysis; Cooperative: operating reserve verification; Green: HPB Report)
- [x] Task: Update checklist filtering logic in DealTabs to include FannieProductType matching
- [x] Task: Add visual indicator for required-but-missing documents per Fannie Mae product type
- [x] Task: Write unit tests for checklist filtering with product type
- [x] Task: Phase 6 Manual Verification [checkpoint: da4ba66]

---
