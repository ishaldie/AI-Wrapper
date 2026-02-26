# AI Wrappers — Product Guidelines

## Prose Style
- **Tone**: Professional but approachable. No jargon.
- **Voice**: Helpful assistant guiding users through document creation.
- **Length**: Concise UI copy. Longer explanations only in help/tooltips.

## Brand Messaging
- Emphasize speed and simplicity: "Professional documents in minutes."
- Focus on output quality, not the AI behind it.
- Avoid over-promising — the AI assists, the user refines.

## UI Principles
- Clean, minimal interface with generous whitespace
- One primary action per screen
- Progressive disclosure — show advanced options only when needed
- Accessible (WCAG 2.1 AA compliance target)

## Error Handling
- User-friendly error messages (never expose stack traces or API errors)
- Graceful degradation when the AI service is unavailable
- Clear loading states during generation (streaming indicator)
