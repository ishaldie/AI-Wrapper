# Spec: Report Assembly & Display

## Overview
Assemble all 10 sections of the underwriting report from calculated metrics, RealAI data, and AI-generated prose into a cohesive Blazor report viewer. Include PDF export capability.

## Requirements
1. Assemble all 10 protocol sections in exact order
2. **Section 1**: Core Investment Metrics table
3. **Section 2**: Executive Summary (AI prose + structured elements)
4. **Section 3**: Underwriting Assumptions table
5. **Section 4**: Property & Sales Comparables (prose + adjustment table)
6. **Section 5**: Tenant & Market Intelligence (benchmarking table + prose)
7. **Section 6**: Operations T12 P&L table with commentary
8. **Section 7**: Financial Analysis (sources & uses, 5-year cash flow, returns, exit)
9. **Section 8**: Value Creation Strategy (AI prose + timeline)
10. **Section 9**: Risk Assessment (matrix table + AI narrative)
11. **Section 10**: Investment Decision (AI prose + structured elements)
12. Blazor report viewer with section navigation
13. Color-coded risk severity (High=red, Medium=orange, Low=green)
14. Decision badge styling (GO=green, CONDITIONAL GO=yellow, NO GO=red)
15. PDF export of complete report
16. Protocol formatting rules: $X,XXX commas, XX.X% percentages, X.XXx multiples

## Acceptance Criteria
- [ ] All 10 sections render correctly in Blazor
- [ ] Section navigation allows jumping between sections
- [ ] Tables display with consistent formatting per protocol rules
- [ ] Risk matrix uses color-coded severity
- [ ] Decision badge prominently displayed in Sections 2 and 10
- [ ] PDF export produces a complete, formatted report
- [ ] Report loads and displays within 3 seconds
- [ ] Data source attribution shown where specified by protocol

## Out of Scope
- Interactive charts/visualizations (can add later)
- Report editing by users
- Report comparison (dashboard track)
