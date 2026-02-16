# Plan: Underwriting Calculation Engine

## Phase 1: Revenue & NOI Calculations
- [x] Create `IUnderwritingCalculator` interface in Domain
- [x] Implement GPR calculation (rent x units x 12)
- [x] Implement vacancy loss (GPR x (1 - occupancy%))
- [x] Implement net rent, other income (13.5%), EGI
- [x] Implement operating expenses (EGI x OpEx ratio or actuals)
- [x] Implement NOI and NOI margin
- [x] Write unit tests for each formula with known values (21 tests, all passing)
- [checkpoint: 2530b77]

## Phase 2: Debt & Returns
- [x] Implement debt amount (price × LTV%)
- [x] Implement debt service for IO and amortizing loans
- [x] Implement equity required (price + acq costs - debt)
- [x] Implement entry cap rate (NOI / price)
- [x] Implement exit cap rate (zipcode cap + 50bps)
- [x] Implement cash-on-cash ((NOI - debt service - reserves) / equity)
- [x] Implement DSCR (NOI / debt service)
- [x] Write unit tests for all debt and return calculations (25 tests, all passing)
- [checkpoint: 3f69b7a]

## Phase 3: Multi-Year Projections & IRR
- [x] Implement 5-year NOI projection with growth assumptions
- [x] Implement annual cash flow series (NOI - debt service - reserves)
- [x] Implement exit value (terminal NOI / exit cap)
- [x] Implement net sale proceeds (exit value - costs - loan balance)
- [x] Implement equity multiple (total distributions / equity)
- [x] Implement IRR using Newton-Raphson method
- [x] Write tests for projection and IRR convergence (20 tests, all passing)
- [checkpoint: 3b7e5b5]

## Phase 4: Comps, Sensitivity & Risk
- [x] Implement sales comp adjustment framework (time, size, age, location, amenities)
- [x] Implement sensitivity scenarios (income -5%, occupancy -10%, cap +100bps)
- [x] Implement risk severity rating logic per protocol thresholds
- [x] Create `CalculationResult` assembler that runs full pipeline
- [x] Write tests for sensitivity deltas and risk rating accuracy (27 tests, all passing)
- [checkpoint: pending]
