# Plan: Deal Entry UI

## Phase 1: Form Components
- [x] Create `DealWizard.razor` multi-step container component
- [x] Create `RequiredInputsStep.razor` (property name, address, units, price)
- [x] Create `PreferredInputsStep.razor` (rent roll summary, T12, loan terms)
- [x] Create `OptionalInputsStep.razor` (hold period, capex, occupancy, value-add)
- [x] Create `DealInputValidator` with FluentValidation rules
- [x] Write bUnit tests for form validation behavior

## Phase 2: Persistence & Navigation
- [x] Create `IDealService` interface in Application layer
- [x] Implement `DealService` with create/update/get operations
- [x] Wire form submission to DealService (save as Draft)
- [x] Create `Deals.razor` list page showing user's deals
- [x] Add "Edit" functionality to load existing deal into wizard
- [x] Write integration tests for deal CRUD operations

## Phase 3: Defaults & UX Polish
- [x] Display protocol default values in optional fields as placeholders
- [x] Show assumption summary panel (what defaults will be used)
- [x] Add "Run Underwriting" button with status transition
- [x] Add form progress indicator (Step 1 of 3)
- [x] Add input formatting (currency, percentage)
- [x] Write tests for default value application
