# Plan: Deal Tabs

## Phase 1: Domain & Database
*Status: Complete*

- [x] Task 1.1: Create new enums — `ChecklistStatus`, `ExecutionType`, `CapitalSource` in Domain/Enums/
- [x] Task 1.2: Create `DealInvestor` entity with all contact fields
- [x] Task 1.3: Create `CapitalStackItem` entity with Source enum, Amount, Rate, TermYears
- [x] Task 1.4: Create `ChecklistTemplate` entity (seeded master list)
- [x] Task 1.5: Create `DealChecklistItem` entity (per-deal status tracking, FK to template + document)
- [x] Task 1.6: Add `ExecutionType` and `TransactionType` properties to Deal entity
- [x] Task 1.7: Add navigation collections to Deal (`DealInvestors`, `CapitalStackItems`, `DealChecklistItems`)
- [x] Task 1.8: Register DbSets + EF configuration in AppDbContext (indexes, relationships, cascades)
- [x] Task 1.9: Create EF migration
- [x] Task 1.10: Seed ChecklistTemplate with all 97 items across 17 sections from Agency DD Checklist

## Phase 2: AI Structured Data Protocol
*Status: Complete*

- [x] Task 2.1: Update `BuildSystemPrompt()` — instruct Claude to emit `deal-update` JSON blocks with general, underwriting, and checklist data
- [x] Task 2.2: Add `ParseDealUpdate()` method — regex extract JSON from Claude response, deserialize to DTO (`DealUpdateParser` in Application/Services)
- [x] Task 2.3: Add `ApplyDealUpdate()` method — update Property, CalculationResult, DealChecklistItem entities from parsed data
- [x] Task 2.4: Add `StripDealUpdateBlocks()` method — remove JSON blocks from displayed message text (also strips in markdown rendering)
- [x] Task 2.5: Wire into `SendInitialAnalysis()` and `SendMessage()` — `ProcessAiResponse()` handles parse, apply, save, and `OnDealUpdated` callback

## Phase 3: Deal Tabs Page & General Tab
*Status: Complete*

- [x] Task 3.1: Create `DealTabs.razor` page at `/deals/{DealId:guid}` with MudTabs (5 tabs)
- [x] Task 3.2: Build General tab content — editable fields for property info (MudTextField/MudNumericField with inline save)
- [x] Task 3.3: Add save logic — debounced field updates to Property + Deal entities
- [x] Task 3.4: Update DealPipeline navigation — row click -> `/deals/{id}` instead of `/deals/{id}/chat`
- [x] Task 3.5: Update back button and breadcrumb navigation

## Phase 4: Underwriting Tab
*Status: Complete*

- [x] Task 4.1: Build Underwriting tab — read-only display of CalculationResult metrics (NOI breakdown, cap rates, returns, debt metrics)
- [x] Task 4.2: Add Capital Stack section — MudTable with add/edit/remove rows (Source, Amount, Rate, Term)
- [x] Task 4.3: Add capital stack save/delete logic
- [x] Task 4.4: Add summary totals (total sources, equity required)

## Phase 5: Checklist Tab
*Status: Complete*

- [x] Task 5.1: Build Checklist tab — load DealChecklistItems grouped by section, filtered by deal ExecutionType/TransactionType
- [x] Task 5.2: Add status dropdown per item (MudSelect with ChecklistStatus enum)
- [x] Task 5.3: Auto-generate DealChecklistItems on deal creation (or first tab visit) from filtered ChecklistTemplate
- [x] Task 5.4: Add document auto-match logic — when a document is uploaded, match to checklist item by keywords, set Satisfied + link DocumentId
- [x] Task 5.5: Add manual document reassignment (user can change which checklist item a doc links to)
- [x] Task 5.6: Visual status indicators (color-coded chips/badges per status)

## Phase 6: Investor Tab
*Status: Complete*

- [x] Task 6.1: Build Investor tab — MudTable listing DealInvestors with add button
- [x] Task 6.2: Add/Edit investor dialog (MudDialog with all contact fields)
- [x] Task 6.3: Delete investor with confirmation
- [x] Task 6.4: Save investor to DB

## Phase 7: Chat Tab Integration
*Status: Complete*

- [x] Task 7.1: Refactor DealChat.razor into `DealChatTab.razor` component (extract from page to component, accept DealId as parameter)
- [x] Task 7.2: Embed DealChatTab in DealTabs as 5th tab
- [x] Task 7.3: Add callback/event — after AI response applies deal-update, notify parent DealTabs to refresh other tabs
- [x] Task 7.4: Keep `/deals/{id}/chat` route working (redirect to `/deals/{id}?tab=chat`)

## Phase 8: Tests & Polish
*Status: Pending*

- [ ] Task 8.1: bUnit tests for DealTabs tab rendering
- [ ] Task 8.2: Unit tests for ParseDealUpdate / StripDealUpdateBlocks
- [ ] Task 8.3: Entity tests for new domain models
- [ ] Task 8.4: Verify all existing tests pass
- [ ] Task 8.5: CSS polish — tab styling consistent with app design system
