# Spec: Market Data Enrichment

## Overview
Implement web search integration to supplement RealAI data with market context: major employers, construction pipeline, infrastructure projects, economic drivers, and current Fannie Mae rates.

## Requirements
1. Web search service for supplemental market data
2. Query patterns per protocol: major employers, development pipeline, economic drivers, infrastructure, comparable transactions, Fannie Mae rates
3. Search results parsed into structured market context data
4. Results cached per deal to avoid redundant searches
5. Market data integrated into Sections 5 (Market Context) and 4 (supplemental comps)
6. Fallback behavior when search returns no useful results
7. Source attribution for all web-sourced data points

## Acceptance Criteria
- [ ] Web search retrieves relevant results for each query pattern
- [ ] Results parsed into structured data (employer names, project descriptions, rates)
- [ ] Data cached per deal
- [ ] Fallback text generated when data unavailable
- [ ] Source URLs tracked for attribution
- [ ] Unit tests for search query construction and result parsing

## Out of Scope
- Real-time market data feeds
- Paid data provider APIs (Bing API, etc.) — use free search for v1
- Historical data archiving
