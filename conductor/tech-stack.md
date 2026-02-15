# AI Wrappers — Tech Stack

## Language
- **Python 3.12+**

## Backend
- **FastAPI** — async web framework for API endpoints and SSE streaming
- **Uvicorn** — ASGI server
- **Pydantic** — request/response validation and settings management

## AI Integration
- **anthropic** (official Python SDK) — Claude API client
- Model: `claude-sonnet-4-5-20250929` (default, configurable)

## Frontend
- **Jinja2** — server-side HTML templates
- **HTMX** — dynamic UI interactions without heavy JS framework
- **Tailwind CSS** (via CDN) — utility-first styling

## Testing
- **pytest** — unit and integration tests
- **pytest-asyncio** — async test support
- **httpx** — async HTTP client for API testing

## Development Tools
- **uv** or **pip** — package management
- **ruff** — linting and formatting
- **python-dotenv** — environment variable management

## Deployment (Future)
- Docker container
- Environment variables for API keys and config

## Project Structure (Target)
```
ai_wrappers/
├── app/
│   ├── __init__.py
│   ├── main.py          # FastAPI app entry point
│   ├── config.py        # Settings and environment
│   ├── routers/         # API route handlers
│   ├── services/        # Business logic (AI generation)
│   ├── models/          # Pydantic models
│   ├── templates/       # Jinja2 HTML templates
│   └── static/          # CSS, JS, images
├── tests/
│   ├── test_api.py
│   └── test_services.py
├── conductor/           # Conductor workflow files
├── pyproject.toml
├── .env.example
└── .gitignore
```
