# Product: ZSR Underwriting Wrapper

## Purpose
An AI-powered web application that automates multifamily real estate acquisition underwriting. Users provide a property address, purchase price, and optional deal documents — the system generates a comprehensive 10-section underwriting report with financial analysis, risk assessment, and GO/NO GO investment decision.

## Problem Solved
Real estate underwriting is time-intensive, requiring analysts to manually pull data from multiple sources, run financial calculations, and write narrative analysis. This wrapper automates the entire process by combining financial calculation engines and Claude AI prose generation into a single workflow that produces institutional-quality reports in minutes.

## Target Users
- **Real estate investors** reviewing multifamily acquisition opportunities
- **Analysts** at ZSR Ventures who need standardized underwriting output
- **Deal reviewers** who want quick initial screening before deep due diligence

## Key Features
- Deal entry with required/optional input collection per the ZSR Underwriting Protocol
- Document upload and parsing (rent rolls, T12/P&L, OMs, appraisals)
- Full calculation engine: NOI, IRR, DSCR, cap rates, 5-year cash flow projections, sensitivity analysis
- Claude AI-generated prose: executive summary, risk narratives, investment thesis, GO/NO GO decision
- 10-section report output with tables, metrics, and formatted analysis
- Market data enrichment via web search (employers, construction pipeline, rates)
- Deal pipeline dashboard with status tracking and saved reports
- PDF export of completed underwriting reports

## Success Metrics
- **Speed**: Generate a complete underwriting report in under 5 minutes
- **Accuracy**: Calculations match manual spreadsheet underwriting within 0.1%
- **Adoption**: All ZSR Ventures deal reviews use the wrapper as the primary screening tool
- **Coverage**: 90%+ of protocol data points populated automatically (calculations + AI)
