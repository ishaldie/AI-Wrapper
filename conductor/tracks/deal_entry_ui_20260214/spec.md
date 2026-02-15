# Spec: Deal Entry UI

## Overview
Build Blazor Server forms for collecting all required and optional user inputs defined in the ZSR Underwriting Protocol. This is the primary data entry point for starting a new underwriting analysis.

## Requirements
1. "New Deal" page with multi-step form wizard
2. Step 1 — Required inputs: property name, address, unit count, purchase price
3. Step 2 — Preferred inputs: rent roll summary, T12 summary, loan terms (LTV, rate, IO/amort, term)
4. Step 3 — Optional inputs: hold period, capex budget, target occupancy, value-add plans
5. Form validation with FluentValidation (required fields enforced, numeric ranges checked)
6. Save as Draft functionality (persist incomplete deals)
7. Edit existing deal inputs
8. Display default assumptions when optional fields are left blank (per protocol defaults)
9. "Run Underwriting" button to trigger the analysis pipeline

## Acceptance Criteria
- [ ] Multi-step form wizard navigates between steps
- [ ] Required fields prevent progression if empty
- [ ] Numeric inputs validate ranges (price > 0, units > 0, LTV 0-100%, etc.)
- [ ] Deal saves to database as Draft status
- [ ] Existing deals can be loaded and edited
- [ ] Default values displayed for optional fields (65% LTV, 5yr hold, 95% occupancy, etc.)
- [ ] "Run Underwriting" transitions deal to InProgress status

## Out of Scope
- Document upload UI (separate track)
- Actual underwriting execution (calculation engine track)
- RealAI data fetching (separate track)
