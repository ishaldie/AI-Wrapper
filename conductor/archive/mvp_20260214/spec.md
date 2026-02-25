# MVP Specification — AI Wrappers

## Overview
Build the foundational project structure and a working end-to-end flow: a user visits the web app, selects "Meeting Notes" as the document type, fills in key details, and receives a polished, AI-generated meeting notes document they can view and copy.

## Requirements

### Functional Requirements

1. **Project Skeleton**
   - `pyproject.toml` with all dependencies
   - FastAPI application entry point
   - Configuration via environment variables (`.env`)
   - Project directory structure per tech-stack.md

2. **Claude Integration Service**
   - Anthropic SDK client wrapper
   - Prompt builder that constructs system + user prompts for document generation
   - Streaming response support (SSE to the frontend)
   - Error handling for API failures (rate limits, timeouts, invalid keys)

3. **Meeting Notes Document Type**
   - Input form collecting: meeting title, date, attendees, agenda items, key discussion points, action items, tone (formal/casual)
   - Structured prompt template that produces well-formatted meeting notes
   - Output includes: header, attendees list, agenda, discussion summary, action items with owners, next steps

4. **Web UI**
   - Landing page with document type selector (only "Meeting Notes" active in MVP)
   - Input form page with guided fields
   - Generation page with streaming output display
   - Result page with formatted document and "Copy to Clipboard" button

5. **Configuration & Security**
   - API key stored in `.env`, loaded server-side only
   - `.env.example` with placeholder values
   - Never expose API key to client

### Non-Functional Requirements
- Streaming responses for perceived speed (SSE)
- Clean, responsive UI (Tailwind CSS)
- Basic error states (API unavailable, invalid input)
- Works in modern browsers (Chrome, Firefox, Safari, Edge)

## Acceptance Criteria
- [ ] `uvicorn app.main:app` starts the server without errors
- [ ] Landing page loads and shows document type options
- [ ] Selecting "Meeting Notes" navigates to the input form
- [ ] Submitting the form streams a generated meeting notes document
- [ ] Generated document is well-structured with all expected sections
- [ ] "Copy to Clipboard" works
- [ ] Missing API key shows a clear error message
- [ ] Tests pass (`pytest`)

## Out of Scope
- User authentication / accounts
- Database / persistence
- Multiple document types (future tracks)
- PDF/DOCX export (future track)
- Deployment / CI/CD
- Revision/editing of generated documents
