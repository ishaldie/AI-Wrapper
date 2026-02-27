# Spec: Underwriting Upgrade — Fannie Mae Compliance

**Track ID:** `underwriting_upgrade_20260226`
**Type:** feature

## Overview

Upgrade the ZSR Underwriting engine to produce Fannie Mae-compliant underwriting for all major multifamily deal types. Currently the app treats all deals with a single DSCR/LTV/amortization profile. The Fannie Mae Multifamily Selling & Servicing Guide and official term sheets define **15+ distinct product types**, each with different LTV caps, DSCR minimums, amortization limits, eligibility rules, and specialized calculations.

This track adds deal-type-aware underwriting profiles so the calculation engine, risk ratings, report prose, and checklist automatically adapt to the selected Fannie Mae product type.

## Current State

- **Deal entity** has `ExecutionType` (All/FannieMae/FreddieMac), `TransactionType` (free-text string), and `PropertyType` (Multifamily/AssistedLiving/SkilledNursing/MemoryCare/CCRC)
- **ProtocolDefaults** has type-aware occupancy and opEx ratios per PropertyType
- **Calculation engine** uses a single DSCR/LTV profile for all deals
- **Risk ratings** use static thresholds (e.g., DSCR < 1.25x = Moderate) regardless of deal type
- **Report prose** switches system role for senior housing but doesn't reference Fannie Mae compliance requirements
- **Checklist** filters by ExecutionType + TransactionType but doesn't enforce product-specific documentation

## Requirements

### R1: Fannie Mae Product Type Model
Add a `FannieProductType` enum and associated configuration that maps each product to its official term sheet parameters:

| Product | Max LTV | Min DSCR | Max Amort | Min Loan | Special Rules |
|---------|---------|----------|-----------|----------|---------------|
| Conventional | 80% | 1.25x | 30yr | — | 90% occ for 90 days |
| SmallLoan | 80% | 1.25x | 30yr | — ($9M cap) | Streamlined ESA |
| AffordableHousing | 80% | 1.20x | 35yr | — | AMI restrictions, sub debt rules |
| SeniorsIL | 75% | 1.30x | 30yr | — | Purpose-built, experienced sponsor |
| SeniorsAL | 75% | 1.40x | 30yr | — | Mgmt + ops reports |
| SeniorsALZ | 75% | 1.45x | 30yr | — | Highest DSCR |
| StudentHousing | 75% | 1.30x (fixed) | 30yr | — | 40%+ student, per-bed option |
| ManufacturedHousing | 80% | 1.25x | 30yr | — | 50+ pads, 5% min vacancy |
| Cooperative | 55% | 1.00x actual / 1.55x market | 30yr | — | Fixed-rate only, dual DSCR |
| SARM | 65%/70% | 1.05x at max rate | 30yr | $25M | Rate cap required |
| GreenRewards | Base+5% | Base | Base | — | 75%/25% savings in NCF |
| Supplemental | 70% | 1.30x | 30yr | — | Combined loan test |
| NearStabilization | 75% | 1.25x (1.15x MAH) | 30yr | $10M | 75% occ at rate lock |
| ROAR | 90% | 1.15x stab | 35yr | $5M | $120K/unit, 50% min occ during rehab |

### R2: Calculation Engine — Product-Aware Underwriting
- `CalculationResultAssembler` uses `FannieProductType` to select correct DSCR min, LTV max, amortization cap
- Add **Seniors Housing tiered DSCR** calculation (IL/AL/ALZ blended based on bed mix)
- Add **Cooperative dual DSCR** test (actual ops 1.00x + market rental basis 1.55x)
- Add **SARM stress test** (DSCR at maximum note rate, not actual rate)
- Add **Green Rewards NCF adjustment** (75% owner + 25% tenant projected savings)
- Add **MHC 5% minimum vacancy floor** in underwriting
- Add **Skilled Nursing NCF cap test** (SNF NCF must be ≤20% of total property NCF)
- Add **ROAR rehab-period DSCR** (1.0x IO / 0.75x amortizing during rehab)
- Add **Supplemental combined loan test** (combined DSCR + combined LTV)

### R3: Risk Rating — Product-Aware Thresholds
- `RiskRatingCalculator.RateDscr()` uses the product's min DSCR as the baseline (not hardcoded 1.25x)
- Add product-specific risk flags:
  - Seniors: SNF NCF > 20% → Critical
  - Student: enrollment < 10K (Dedicated) → High
  - MHC: tenant-occupied > 35% → High
  - Co-op: single-sponsor > 40% → Moderate
  - Affordable: hard sub debt combined DSCR < 1.05x → Critical

### R4: Report Prose — Fannie Mae Compliance Context
- `UnderwritingPromptBuilder` includes the deal's Fannie product type and compliance requirements in prompts
- Risk Assessment prompt references product-specific thresholds
- Investment Decision prompt uses product-aware GO/NO GO thresholds (not just IRR > 15% + DSCR > 1.5x)
- Executive Summary identifies the Fannie Mae execution type and product

### R5: Deal Entry UI — Product Type Selection
- Add `FannieProductType` dropdown on Deal Entry (visible when ExecutionType = FannieMae)
- Auto-suggest product type based on PropertyType mapping:
  - Multifamily → Conventional (default)
  - AssistedLiving → SeniorsAL
  - SkilledNursing → SeniorsIL (since SNF alone isn't eligible, must be combo)
  - MemoryCare → SeniorsALZ
  - CCRC → SeniorsIL
- Show product-specific fields conditionally (e.g., student enrollment, MHC pad count, Green improvement budget)

### R6: Compliance Checklist Enhancement
- Map `FannieProductType` to required third-party reports and documentation
- Flag missing required items per product type (e.g., Seniors needs Mgmt + Ops + Regulatory Compliance reports)
- Show product-specific documentation requirements on the checklist tab

## Acceptance Criteria

- [ ] `FannieProductType` enum with 14 values persisted on Deal entity
- [ ] Each product type has a configuration record defining LTV, DSCR, amortization, eligibility rules
- [ ] Calculation engine applies correct DSCR min and LTV max per product type
- [ ] Seniors Housing DSCR correctly blends IL (1.30x) / AL (1.40x) / ALZ (1.45x) based on bed mix
- [ ] Cooperative dual DSCR test passes both actual (1.00x) and market rental (1.55x)
- [ ] SARM deals test DSCR at maximum note rate
- [ ] Green Rewards applies 75%/25% energy savings to NCF
- [ ] MHC enforces 5% minimum vacancy
- [ ] SNF NCF ≤20% test flagged in risk assessment
- [ ] Risk ratings use product-specific thresholds
- [ ] Report prose references Fannie Mae product type and compliance requirements
- [ ] Deal Entry UI shows product type selector when ExecutionType = FannieMae
- [ ] Checklist shows product-specific required documents
- [ ] All existing tests continue to pass (no regressions)

## Out of Scope

- Freddie Mac product types (future track)
- Automated Form 4660 tier classification and pricing (requires Fannie Mae lender portal data)
- Bond execution mechanics (M.TEB, Credit Enhancement) — complex structured products
- Forward Commitment / construction draw management
- Bulk Delivery / Credit Facility portfolio-level underwriting
- PDF term sheet generation matching Fannie Mae format
- Automated submission to DUS lender systems
