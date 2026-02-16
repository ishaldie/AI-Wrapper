# Plan: Dashboard & Deal Pipeline

## Phase 1: Dashboard Home
- [x] Create `Dashboard.razor` home page
- [x] Implement summary statistics cards (total, active, completed deals)
- [x] Create recent activity feed component `b579039`
- [x] Add "New Deal" action button linking to deal wizard `b579039`
- [x] Write bUnit tests for dashboard components `b579039`

## Phase 2: Deal Pipeline Table
- [x] Create `DealPipeline.razor` with MudBlazor DataGrid `8368bfe`
- [x] Implement sortable columns (name, address, status, date, cap rate, IRR) `8368bfe`
- [x] Implement status filter chips (Draft, InProgress, Complete, Archived) `8368bfe`
- [x] Implement search by property name and address `8368bfe`
- [x] Create `DealCard.razor` quick-view component with key metrics `8368bfe`
- [x] Write tests for filtering and sorting logic `8368bfe`

## Phase 3: Comparison & Management
- [x] Create `DealComparison.razor` side-by-side view `c474673`
- [x] Implement deal selection (checkboxes) for comparison `c474673`
- [x] Display comparison table with aligned metrics `c474673`
- [x] Implement archive deal functionality `c474673`
- [x] Implement delete deal with confirmation dialog `c474673`
- [x] Write integration tests for pipeline operations `c474673`
