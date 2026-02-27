# Spec: Securitization Comps — SEC EDGAR + Agency Data

## Overview
Pull loan-level data from CMBS securitizations (SEC EDGAR ABS-EE filings) and Fannie Mae/Freddie Mac multifamily loan performance datasets to provide market-comparable benchmarks alongside user underwriting. When a deal is underwritten, the system fetches comparable securitized loans by MSA, property type, and size — then displays a side-by-side comparison of the user's assumptions vs actual securitized deal metrics.

## Problem
Underwriters currently have no automated way to benchmark their assumptions against real securitized loan data. They manually research CMBS deals or rely on broker quotes. This feature automates comp-pulling from three public data sources to validate DSCR, LTV, cap rate, and occupancy assumptions against actual market transactions.

## Data Sources

### 1. SEC EDGAR — CMBS EX-102 (Primary)
- **Endpoint**: `data.sec.gov` REST API (free, no auth)
- **Data**: ABS-EE filings with EX-102 XML exhibits containing loan-level CMBS data
- **Key fields**: `property_type_code` (MF, HC, MH), `most_recent_dsc_noi_percentage`, `most_recent_noi_amount`, `most_recent_valuation_amount`, `original_loan_amount`, `original_interest_rate_percentage`, `most_recent_physical_occupancy_percentage`, `units_beds_rooms`, `property_state`, `property_city`
- **Coverage**: All CMBS deals filed since Nov 2016

### 2. Fannie Mae — Multifamily Loan Performance Data
- **Source**: CSV download from Data Dynamics (free registration required)
- **Data**: 71K+ loans, 62 fields including underwritten DSCR, LTV, property type, location
- **Access**: Bulk CSV download, pre-processed and stored locally
- **Refresh**: Quarterly

### 3. Freddie Mac — K-Deal Performance
- **Source**: MSIA tool / Securities Lookup (free)
- **Data**: K-Series multifamily loan-level data
- **Access**: Bulk download or web scrape
- **Refresh**: Monthly

## Requirements
1. Create a `SecuritizationCompService` that queries SEC EDGAR for CMBS comps by property type + state/MSA
2. Parse EX-102 XML to extract loan-level metrics (DSCR, LTV, NOI, occupancy, rate, units)
3. Import Fannie Mae MFLPD CSV into a local `SecuritizationComp` table for fast querying
4. Build comp-matching logic: same property type, same state (or within 100mi), similar unit count (±50%), recent vintage (last 3 years)
5. Display comparison table on the Analysis/Underwriting tab showing user metrics vs market comps
6. Include comp data in Claude AI prompts so report prose can reference market positioning
7. Support manual refresh of the local comp database

## Technical Approach
- New domain entity: `SecuritizationComp` (loan-level comp record from any source)
- New service: `SecuritizationCompService` (fetch, parse, match)
- New service: `EdgarCmbsClient` (HTTP client for SEC EDGAR API + XML parsing)
- New service: `AgencyDataImporter` (CSV import for Fannie Mae / Freddie Mac bulk data)
- UI: New comparison card on DealTabs Underwriting tab
- Prompt update: Include top 5 comps in relevant prompts

## Acceptance Criteria
- Given a multifamily deal in Atlanta, the system returns 5+ CMBS comps from GA within the last 3 years
- Comp table displays: DSCR, LTV, Cap Rate, Occupancy, Loan Amount, Rate, Units — with user's deal highlighted
- Report prompts include comp benchmarks so Claude references market positioning
- Fannie Mae CSV import processes 71K+ loans without timeout
- All existing tests pass + new tests for comp matching, XML parsing, CSV import

## Out of Scope
- Real-time streaming of SEC filings (batch/on-demand only)
- Freddie Mac MSIA API integration (manual CSV import first, API later)
- Historical trend analysis across securitization vintages
- Comp data for non-multifamily property types (future enhancement)
