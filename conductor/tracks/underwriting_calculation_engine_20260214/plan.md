# Plan: Underwriting Calculation Engine

## Phase 1: Revenue & NOI Calculations
- [x] Create `IUnderwritingCalculator` interface in Domain
- [x] Implement GPR calculation (rent x units x 12)
- [x] Implement vacancy loss (GPR x (1 - occupancy%))
- [x] Implement net rent, other income (13.5%), EGI
- [x] Implement operating expenses (EGI x OpEx ratio or actuals)
- [x] Implement NOI and NOI margin
- [x] Write unit tests for each formula with known values (19 tests, all passing)

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
