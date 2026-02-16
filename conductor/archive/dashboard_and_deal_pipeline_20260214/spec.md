# Spec: Dashboard & Deal Pipeline

## Overview
Build the main application dashboard with a deal pipeline view, saved reports, status tracking, and deal comparison tools.

## Requirements
1. Dashboard home page with summary statistics (total deals, active, completed)
2. Deal pipeline table with sortable columns (name, address, status, date, key metrics)
3. Status filters: Draft, InProgress, Complete, Archived
4. Quick-view deal card showing key metrics (price, cap rate, IRR, decision badge)
5. Deal comparison view: side-by-side metrics for 2-3 deals
6. Search and filter by property name, address, date range
7. "New Deal" quick-action button
8. Recent activity feed (last 10 deals created/updated)
9. Archive and delete deal functionality

## Acceptance Criteria
- [ ] Dashboard loads with summary statistics
- [ ] Deal pipeline table displays all user's deals
- [ ] Sorting works on all columns
- [ ] Status filters show correct deals
- [ ] Deal card shows key metrics and decision badge
- [ ] Comparison view renders 2-3 deals side by side
- [ ] Search filters by name and address
- [ ] Archive moves deal to archived status
- [ ] Delete removes deal (with confirmation)

## Out of Scope
- Team/shared deals (single-user for v1)
- Export deal list to CSV
- Analytics/reporting on deal pipeline
