# Spec: Deal Tabs (AI-Driven)

## Overview
Replace the standalone DealChat page with a tabbed deal view at `/deals/{id}`. The AI chat is the **primary data engine** — as Claude researches a property, it emits structured JSON blocks (`deal-update`) alongside its conversational response. The server parses these blocks and populates the deal's General, Underwriting, and Checklist tabs automatically. Users can manually override any AI-populated field. The Investor tab is fully manual.

## Requirements

1. **Deal Tabs Page** (`/deals/{id}`) — MudTabs: General, Underwriting, Investors, Checklist, Chat
2. **AI Structured Data Protocol** — System prompt instructs Claude to emit `deal-update` JSON in every response. Server extracts, applies to entities, strips from displayed message.
3. **General Tab** — Shows property info (name, address, units, year built, type, sqft, acreage). AI-populated, user-editable. Saves on change.
4. **Underwriting Tab** — Shows NOI breakdown, key metrics, and new Capital Stack (senior debt, mezz, pref equity, sponsor equity). AI-populated, user-editable.
5. **Investor Tab** — Manual CRUD. Each investor: name, company, role, address, phone, email, net worth, liquidity, notes.
6. **Checklist Tab** — Seeded from Agency DD Property Checklist (~97 items, 17 sections). Filtered by deal's execution type + transaction type. Statuses: Outstanding, Under Review, Need Additional Info, Waiver Requested, Waiver Approved, Satisfied, N/A.
7. **Auto-match uploads** — When docs uploaded (chat or checklist tab), match to checklist item by name/type, mark Satisfied. User can reassign.
8. **Chat Tab** — Existing DealChat functionality refactored into an embeddable tab component. After each AI response, other tabs refresh.
9. **Navigation** — Pipeline row click -> `/deals/{id}` (General tab). Back button -> `/deals`.

## New Domain Entities

- **DealInvestor** — DealId, Name, Company, Role, Address, City, State, Zip, Phone, Email, NetWorth (decimal?), Liquidity (decimal?), Notes
- **CapitalStackItem** — DealId, Source (enum), Amount, Rate, TermYears, Notes, SortOrder
- **ChecklistTemplate** — Id, Section, SectionOrder, ItemName, SortOrder, ExecutionType (enum), TransactionType (string)
- **DealChecklistItem** — DealId, ChecklistTemplateId, Status (enum), DocumentId? (FK -> UploadedDocument), Notes, UpdatedAt

## New Enums

- **ChecklistStatus** — Outstanding, UnderReview, NeedAdditionalInfo, WaiverRequested, WaiverApproved, Satisfied, NotApplicable
- **ExecutionType** — All, FannieMae, FreddieMac
- **CapitalSource** — SeniorDebt, Mezzanine, PreferredEquity, SponsorEquity, Other

## Deal Entity Changes

- Add `ExecutionType` (enum, default All) and `TransactionType` (string, default "All") to Deal

## Structured Data Protocol

Claude includes in responses:
```json
{
  "general": {"yearBuilt": 1985, "buildingType": "Garden", "squareFootage": 45000},
  "underwriting": {"grossPotentialRent": 600000, "noi": 342000, "goingInCapRate": 6.84},
  "checklist": [{"item": "Current Months Rent Roll", "status": "Outstanding"}]
}
```
Server parses -> updates Property, CalculationResult, DealChecklistItem -> strips block from displayed message.

## Acceptance Criteria

- [ ] `/deals/{id}` renders 5 tabs; tab state persists during session
- [ ] AI responses auto-populate General + Underwriting + Checklist tabs
- [ ] User can edit any AI-populated field (override)
- [ ] Investor CRUD works (add/edit/remove)
- [ ] Checklist filtered by deal's execution/transaction type
- [ ] Document uploads auto-match to checklist items
- [ ] DealPipeline navigates to `/deals/{id}`
- [ ] Existing tests pass; new tests cover tab rendering

## Out of Scope

- Credit DD Checklist (Property DD only)
- Multi-property portfolio columns (single property)
- 3rd party routing (Submit Documents To columns)
- PDF export of checklist
