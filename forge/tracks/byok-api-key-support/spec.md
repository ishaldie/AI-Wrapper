# Spec: BYOK API Key Support

## Overview
Allow users to provide their own Anthropic API key so they can use the underwriting platform with their own Claude account billing. Keys can be entered via direct paste or JSON credentials file upload. Keys are encrypted at rest using ASP.NET Data Protection.

## Requirements
1. Users can save their own Anthropic API key, encrypted at rest via ASP.NET Data Protection
2. Users can upload a JSON credentials file (`{"api_key": "...", "model": "...", "label": "..."}`) as an alternative to pasting
3. The system resolves API keys per-request: user's BYOK key takes priority, falls back to shared platform key
4. Users with a BYOK key can optionally set a preferred Claude model
5. API key validation via a lightweight test request before saving
6. Account settings page with masked key display, test connection, save, and remove actions
7. BYOK users bypass daily token budget limits (per-deal budget still applies)
8. Token usage records track whether each call used a BYOK key for cost attribution
9. Admin dashboard shows BYOK status per user and can filter usage by key source

## Acceptance Criteria
- Saving and retrieving a key round-trips correctly through encryption
- JSON parser handles both minimal and full formats, rejects malformed input
- ClaudeClient uses BYOK key when available, falls back to shared key
- Settings page shows masked key, allows test/save/remove
- BYOK users are not blocked by daily budget limits
- All Claude calls from all entry points (chat, report prose, sales comp extraction) respect BYOK resolution

## Out of Scope
- OAuth-based Anthropic account linking (Anthropic does not offer OAuth)
- Key rotation or expiration policies
- Per-user rate limiting beyond existing budget system
- Multi-provider support (OpenAI, etc.)
