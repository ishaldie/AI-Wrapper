# Spec: RealAI API Integration

## Overview
Build an HTTP client service to query the RealAI API (app.realai.com) for property, tenant, market, and comparable sales data. Cache responses and map RealAI fields to domain entities.

## Requirements
1. HTTP client with authentication for RealAI API
2. Query property data: in-place rent, occupancy, year built, acreage, sqft, amenities, building type
3. Query tenant metrics: avg FICO, rent-to-income ratio, median HHI (subject, zipcode, metro levels)
4. Query market data: cap rates, rent growth, job growth, net migration, permits
5. Query sales comparables: price/unit, sale date, units, condition
6. Query rent/occupancy time series for trend charts
7. Response caching to avoid redundant API calls (cache per deal, TTL: 24 hours)
8. Retry logic with Polly for transient failures
9. Map RealAI response fields to `RealAiData` entity
10. Handle API errors gracefully with fallback to "data unavailable" flags

## Acceptance Criteria
- [ ] RealAI client authenticates and retrieves property data by address
- [ ] All data points listed in the protocol's RealAI table are mapped
- [ ] Responses cached per deal with 24-hour TTL
- [ ] Transient failures retry up to 3 times with exponential backoff
- [ ] API errors logged and surfaced as "data unavailable" (not app crash)
- [ ] Unit tests mock RealAI responses and verify mapping
- [ ] Integration test confirms end-to-end API call (with real credentials)

## Out of Scope
- Web scraping / browser automation (API-only approach)
- Real-time data streaming
- RealAI supplemental reports (future tracks)
