# Code Style Guides

## Python
- Follow PEP 8, enforced by `ruff`
- Use type hints for all public function signatures
- Prefer f-strings over `.format()` or `%`
- Use `pathlib.Path` over `os.path`
- Async functions where possible (FastAPI is async-first)
- Import order: stdlib, third-party, local (enforced by ruff)

## HTML/Templates
- Jinja2 templates with consistent 2-space indentation
- Use HTMX attributes for dynamic behavior
- Semantic HTML elements

## CSS
- Tailwind utility classes (via CDN)
- No custom CSS unless absolutely necessary
