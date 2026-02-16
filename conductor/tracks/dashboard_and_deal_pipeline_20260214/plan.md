# Plan: Dashboard & Deal Pipeline

## Phase 1: Dashboard Home
- [x] Create `Dashboard.razor` home page
- [x] Implement summary statistics cards (total, active, completed deals)
- [x] Create recent activity feed component `b579039`
- [x] Add "New Deal" action button linking to deal wizard `b579039`
- [x] Write bUnit tests for dashboard components `b579039`

## Phase 2: Deal Pipeline Table
- [ ] Create `DealPipeline.razor` with MudBlazor DataGrid
- [ ] Implement sortable columns (name, address, status, date, cap rate, IRR)
- [ ] Implement status filter chips (Draft, InProgress, Complete, Archived)
- [ ] Implement search by property name and address
- [ ] Create `DealCard.razor` quick-view component with key metrics
- [ ] Write tests for filtering and sorting logic

## Phase 3: Comparison & Management
- [ ] Create `DealComparison.razor` side-by-side view
- [ ] Implement deal selection (checkboxes) for comparison
- [ ] Display comparison table with aligned metrics
- [ ] Implement archive deal functionality
- [ ] Implement delete deal with confirmation dialog
- [ ] Write integration tests for pipeline operations
