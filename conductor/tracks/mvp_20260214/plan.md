# MVP Implementation Plan

## Phase 1: Project Setup
- [ ] Create `pyproject.toml` with dependencies (fastapi, uvicorn, anthropic, jinja2, python-dotenv, httpx, pytest, pytest-asyncio, ruff)
- [ ] Create project directory structure (`app/`, `app/routers/`, `app/services/`, `app/models/`, `app/templates/`, `app/static/`, `tests/`)
- [ ] Create `app/__init__.py` and `app/main.py` with minimal FastAPI app
- [ ] Create `app/config.py` with Pydantic settings (API key, model name, etc.)
- [ ] Create `.env.example` with placeholder values
- [ ] Create `.gitignore` (Python standard + .env)
- [ ] Verify: `uvicorn app.main:app` starts and serves a health check endpoint

## Phase 2: Claude Integration Service
- [ ] Write tests for the generation service (mock Anthropic client)
- [ ] Create `app/services/ai_generator.py` — Anthropic client wrapper with streaming support
- [ ] Create `app/models/document.py` — Pydantic models for document request/response
- [ ] Create `app/services/prompts.py` — prompt template builder for meeting notes
- [ ] Implement SSE streaming endpoint in a router
- [ ] Verify: tests pass, streaming endpoint returns mock data

## Phase 3: Meeting Notes Form & UI
- [ ] Write tests for the meeting notes API endpoint
- [ ] Create `app/routers/documents.py` — routes for form display and generation
- [ ] Create base HTML template (`app/templates/base.html`) with Tailwind CSS and HTMX
- [ ] Create landing page template (`app/templates/index.html`) with document type cards
- [ ] Create meeting notes input form template (`app/templates/meeting_notes_form.html`)
- [ ] Create generation/result template (`app/templates/result.html`) with SSE streaming display
- [ ] Add "Copy to Clipboard" JavaScript functionality
- [ ] Verify: full flow works end-to-end in browser

## Phase 4: Polish & Validation
- [ ] Add input validation (required fields, reasonable limits)
- [ ] Add error handling UI (API unavailable, invalid key, generation failure)
- [ ] Add loading/streaming indicator
- [ ] Responsive layout check (desktop + tablet)
- [ ] Run `ruff check` and `ruff format` — fix any issues
- [ ] Run full test suite — all tests pass
- [ ] Manual end-to-end test with real API key
