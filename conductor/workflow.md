# AI Wrappers — Development Workflow

## Branch Strategy
- `main` — stable, deployable code
- Feature branches: `feature/<track-id>` off `main`
- Commit often with descriptive messages

## Commit Convention
```
type(scope): description

Types: feat, fix, refactor, test, docs, chore
Scope: api, ui, ai, config, etc.
```

## Development Flow
1. Read conductor context files before starting work
2. Follow the active track's plan.md
3. Write tests alongside implementation (test-adjacent, not strict TDD)
4. Run `ruff check` and `ruff format` before committing
5. Run `pytest` to verify no regressions

## Testing Strategy
- Unit tests for services and models
- Integration tests for API endpoints
- Test coverage target: 70%+
- Mock the Anthropic API in tests (never call real API in tests)

## Code Quality
- Follow PEP 8 via ruff
- Type hints on all public functions
- Docstrings on modules and public classes
- No hardcoded secrets — use environment variables

## Review Checklist
- [ ] Tests pass
- [ ] Linting passes
- [ ] No secrets in code
- [ ] UI works on desktop and tablet viewports
- [ ] Error states handled gracefully
