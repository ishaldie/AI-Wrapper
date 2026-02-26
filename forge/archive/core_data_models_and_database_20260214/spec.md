# Spec: Core Data Models & Database

## Overview
Define all domain entities, value objects, and database schema for the underwriting application. This includes deals, properties, underwriting inputs, calculation results, and reports.

## Requirements
1. `Deal` entity — top-level container (name, status, created/updated, user reference)
2. `Property` entity — address, unit count, year built, building type, acreage, sqft
3. `UnderwritingInput` entity — purchase price, LTV, loan terms, hold period, target occupancy, capex budget, value-add plans
4. `LoanTerms` value object — LTV%, interest rate, IO period, amortization term, loan amount
5. `RealAiData` entity — cached RealAI response data (rents, occupancy, FICO, HHI, cap rates, comps, market data)
6. `CalculationResult` entity — all computed metrics (NOI, IRR, DSCR, cash flows, exit value, etc.)
7. `UnderwritingReport` entity — final assembled report with all 10 sections, GO/NO GO decision
8. `UploadedDocument` entity — file reference, type (rent roll, T12, OM, etc.), parsed data
9. Configure EF Core relationships and indexes
10. Create initial migration and verify schema

## Acceptance Criteria
- [ ] All entities defined with proper relationships
- [ ] EF Core migration creates schema successfully
- [ ] CRUD operations work for Deal and related entities
- [ ] Deal status enum: Draft, InProgress, Complete, Archived
- [ ] Cascading delete: deleting a Deal removes all child records
- [ ] Unit tests verify entity validation rules

## Out of Scope
- Business logic / calculations (separate track)
- UI for creating/editing (separate track)
- RealAI API calls (separate track)
