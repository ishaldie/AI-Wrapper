# Spec: Claude AI Report Generation

## Overview
Integrate the Claude API (Anthropic) to generate the prose sections of the underwriting report: executive summary, investment thesis, market context, value creation strategy, risk narratives, and GO/CONDITIONAL GO/NO GO investment decision.

## Requirements
1. Claude API HTTP client with authentication
2. Structured prompts that pass calculated metrics + RealAI data as context
3. Generate **Executive Summary** with decision badge, one-line summary, investment thesis, returns snapshot, top 3 risks
4. Generate **Market Context** prose (supply-demand, economic drivers, rent growth outlook)
5. Generate **Value Creation Strategy** with execution timeline and capital requirements
6. Generate **Risk Assessment** narratives supporting the risk matrix data
7. Generate **Investment Decision** with GO/CONDITIONAL GO/NO GO, conditions precedent, required adjustments, next steps
8. Generate **Property Overview** prose paragraph
9. Prompt engineering to ensure consistent output format matching protocol structure
10. Token usage tracking and cost estimation per report
11. Retry logic for API failures

## Acceptance Criteria
- [ ] Claude API client authenticates and sends/receives messages
- [ ] All 6 prose sections generate successfully with real data
- [ ] Output matches protocol format (decision badge, structured elements)
- [ ] GO/NO GO decision logic aligns with protocol thresholds (IRR >15%, DSCR >1.5x)
- [ ] Prompts include all relevant data points from calculations and RealAI
- [ ] Token usage logged per request
- [ ] API failures handled with retry and user notification
- [ ] Unit tests verify prompt construction and response parsing

## Out of Scope
- Streaming responses (batch generation is fine for v1)
- User editing of AI-generated prose (future enhancement)
- Multiple AI provider support (Claude only for now)
