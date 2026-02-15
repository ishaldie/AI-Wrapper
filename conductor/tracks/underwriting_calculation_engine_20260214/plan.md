# Plan: Underwriting Calculation Engine

## Phase 1: Revenue & NOI Calculations
- [ ] Create `IUnderwritingCalculator` interface in Domain
- [ ] Implement GPR calculation (rent × units × 12)
- [ ] Implement vacancy loss (GPR × (1 - occupancy%))
- [ ] Implement net rent, other income (13.5%), EGI
- [ ] Implement operating expenses (EGI × OpEx ratio or actuals)
- [ ] Implement NOI and NOI margin
- [ ] Write unit tests for each formula with known values

## Phase 2: Debt & Returns
- [ ] Implement debt amount (price × LTV%)
- [ ] Implement debt service for IO and amortizing loans
- [ ] Implement equity required (price + acq costs - debt)
- [ ] Implement entry cap rate (NOI / price)
- [ ] Implement exit cap rate (zipcode cap + 50bps)
- [ ] Implement cash-on-cash ((NOI - debt service - reserves) / equity)
- [ ] Implement DSCR (NOI / debt service)
- [ ] Write unit tests for all debt and return calculations

## Phase 3: Multi-Year Projections & IRR
- [ ] Implement 5-year NOI projection with growth assumptions
- [ ] Implement annual cash flow series (NOI - debt service - reserves)
- [ ] Implement exit value (terminal NOI / exit cap)
- [ ] Implement net sale proceeds (exit value - costs - loan balance)
- [ ] Implement equity multiple (total distributions / equity)
- [ ] Implement IRR using Newton-Raphson method
- [ ] Write tests for projection and IRR convergence

## Phase 4: Comps, Sensitivity & Risk
- [ ] Implement sales comp adjustment framework (time, size, age, location, amenities)
- [ ] Implement sensitivity scenarios (income -5%, occupancy -10%, cap +100bps)
- [ ] Implement risk severity rating logic per protocol thresholds
- [ ] Create `CalculationResult` assembler that runs full pipeline
- [ ] Write tests for sensitivity deltas and risk rating accuracy
