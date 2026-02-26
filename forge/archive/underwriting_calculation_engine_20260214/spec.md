# Spec: Underwriting Calculation Engine

## Overview
Implement all financial calculation formulas defined in the ZSR Underwriting Protocol. This is the core computation layer that takes user inputs + RealAI data and produces all derived metrics.

## Requirements
1. **Revenue calculations**: GPR, vacancy loss, net rent, other income (13.5%), EGI
2. **Expense calculations**: operating expenses (54.35% default or actuals), NOI, NOI margin
3. **Cap rate calculations**: entry cap rate, exit cap rate (zipcode + 50bps)
4. **Debt calculations**: debt amount, annual debt service (IO and amortizing), equity required, acquisition costs (2%)
5. **Return calculations**: cash-on-cash, DSCR, reserves ($250/unit/year)
6. **Exit calculations**: exit value, sale costs (2%), net sale proceeds, equity multiple, IRR
7. **5-year cash flow projection** with growth assumptions (0% Y1-2, 1.5% Y3-5)
8. **Sales comp adjustment framework**: time, size, age/condition, location, amenities
9. **Sensitivity analysis**: NOI scenarios (base, -5% income, -10% occupancy, +100bps cap rate)
10. **Risk severity ratings**: based on data thresholds from protocol (rent premium, FICO gap, occupancy gap)
11. All calculations must be pure functions with no side effects
12. Results rounded per protocol formatting rules (1 decimal %, 2 decimal multiples)

## Acceptance Criteria
- [ ] All core formulas produce correct results against manual spreadsheet verification
- [ ] IRR calculation converges for standard cash flow scenarios
- [ ] 5-year projection applies correct growth rates per year
- [ ] Sensitivity scenarios produce expected deltas from base case
- [ ] Risk ratings match protocol thresholds
- [ ] User-provided actuals override defaults when present
- [ ] Every calculation has a unit test with known inputs/outputs
- [ ] Edge cases handled: zero units, zero price, negative NOI

## Out of Scope
- Prose generation (Claude AI track)
- Report formatting (report assembly track)
- Data fetching (RealAI and deal entry tracks)
