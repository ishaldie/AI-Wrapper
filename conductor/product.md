# AI Wrappers — Product Definition

## Vision
A web-based business document generator powered by Anthropic's Claude API. Non-technical end users can quickly produce professional business documents — reports, proposals, meeting notes, SOPs, and more — by providing simple inputs and selecting a document type.

## Target Users
- **Primary**: Business professionals, managers, and team leads who need to produce documents frequently but lack time or writing expertise.
- **Secondary**: Small business owners and freelancers who need professional-quality documents without hiring writers.

## Core Features
1. **Document Type Selection** — Users pick from a catalog of business document templates (report, proposal, meeting notes, SOP, memo, executive summary, etc.)
2. **Guided Input** — A simple form collects key details (topic, audience, tone, key points, length) tailored to each document type.
3. **AI Generation** — Claude generates a polished, structured document based on the inputs.
4. **Live Preview & Edit** — Users review the generated document in-browser and can request revisions or manually edit.
5. **Export** — Download finished documents as Markdown, PDF, or DOCX.

## Non-Functional Requirements
- Fast generation (streaming responses for perceived speed)
- Clean, minimal UI suitable for non-technical users
- Secure API key handling (server-side only, never exposed to client)
- Responsive design (works on desktop and tablet)

## Success Metrics
- Users can generate a usable business document in under 2 minutes
- Generated documents require minimal manual editing
- App is intuitive enough to use without a tutorial
