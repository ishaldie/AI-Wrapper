# CGX: Document Upload Security Hardening

## Session Log

### Session 1 — 2026-02-22
- **Context**: Security audit revealed 7 critical gaps in document upload pipeline
- **Key Decision**: Address all 7 issues in one track (multi-tenant, validation, scanning, rate limiting, audit)
- **Discovery**: Deal entity has no UserId — any authenticated user can access any deal
- **Discovery**: Extension-only validation, no magic byte or MIME checking
- **Discovery**: CSV/XLSX parsers don't sanitize formula injection
- **Track created**: 4 phases, ~30 tasks

## Learnings

_To be updated during implementation._

## Friction Points

_To be updated during implementation._
